import { useEffect, useState } from "react";
import { Container, Typography, Table, TableRow, TableCell, TableHead, TableBody, Button } from "@mui/material";
import { useNavigate } from "react-router-dom";
import api from "../api/apiClient";
import { jwtDecode } from "jwt-decode";

export default function AccountsPage() {

  const [accounts, setAccounts] = useState([]);
  const navigate = useNavigate();

  const token = localStorage.getItem("token");
  let role = "";
  if (token) {
    const decoded = jwtDecode(token);
    role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  }

  useEffect(() => {
    loadAccounts();
  }, []);

  const loadAccounts = async () => {
    const res = await api.get("/ledger/accounts");
    setAccounts(res.data);
  };

  const checkBalance = async (id) => {
    try {
      const res = await api.get(`/ledger/balance/${id}`);
      alert(`Account Balance for ${res.data.accountName}:\n$${res.data.balance.toFixed(2)}`);
    } catch (err) {
      alert("Failed to check balance: " + (err.response?.data || err.message));
    }
  };

  const unfreeze = async (id) => {
    await api.post(`/ledger/accounts/${id}/unfreeze`, {
      reason: "Manual review completed"
    });
    loadAccounts();
  };

  const getStatusText = (status) => {
    const mapping = {
      0: "Active",
      1: "Debit Frozen",
      2: "Fully Frozen"
    };
    return mapping[status] || status;
  };

  return (
    <Container>
      <Typography variant="h4" sx={{ mt: 3 }}>Accounts</Typography>

      <Table sx={{ mt: 3 }}>
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Actions</TableCell>
            <TableCell></TableCell>
            <TableCell></TableCell>
          </TableRow>
        </TableHead>

        <TableBody>
          {accounts.map(a => (
            <TableRow key={a.accountId}>
              <TableCell>{a.accountName}</TableCell>
              <TableCell>{getStatusText(a.status)}</TableCell>
              <TableCell>
                <Button
                  size="small"
                  variant="outlined"
                  onClick={() => navigate(`/accounts/${a.accountId}/statement`)}
                >
                  View Statement
                </Button>
              </TableCell>
              <TableCell>
                <Button
                  size="small"
                  variant="outlined"
                  color="info"
                  onClick={() => checkBalance(a.accountId)}
                >
                  Check Balance
                </Button>
              </TableCell>
              <TableCell>
                {a.status !== 0 && role === "Admin" && (
                  <Button
                    size="small"
                    variant="contained"
                    color="warning"
                    onClick={() => unfreeze(a.accountId)}
                  >
                    Unfreeze
                  </Button>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Container>
  );
}