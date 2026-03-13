import { useEffect, useState } from "react";
import { Container, Typography, Table, TableRow, TableCell, TableHead, TableBody, Button, Alert } from "@mui/material";
import api from "../api/apiClient";
import { jwtDecode } from "jwt-decode";

export default function TransactionsPage() {
  const [transactions, setTransactions] = useState([]);
  const [error, setError] = useState(null);

  const token = localStorage.getItem("token");
  let role = "";
  if (token) {
    const decoded = jwtDecode(token);
    role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  }

  useEffect(() => {
    loadTransactions();
  }, []);

  const loadTransactions = async () => {
    try {
      const res = await api.get("/ledger/transactions?page=1&pageSize=20");
      setTransactions(res.data.items);
    } catch (err) {
      setError(err.response?.data || "Error loading transactions.");
    }
  };

  const reverseTransaction = async (id) => {
    try {
      await api.post(`/ledger/reverse/${id}`);
      alert("Transaction reversed successfully.");
      loadTransactions();
    } catch (err) {
      alert("Failed to reverse: " + (err.response?.data || err.message));
    }
  };

  return (
    <Container>
      <Typography variant="h4" sx={{ mt: 3, mb: 3 }}>Transactions</Typography>

      {error ? (
        <Alert severity="error">{error}</Alert>
      ) : (
        <Table sx={{ mt: 3 }}>
          <TableHead>
            <TableRow>
              <TableCell>Description</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Type</TableCell>
              <TableCell>Date</TableCell>
              {(role === "Admin" || role === "Operator") && (
                <TableCell>Actions</TableCell>
              )}
            </TableRow>
          </TableHead>

          <TableBody>
            {transactions.map(t => (
              <TableRow key={t.transactionId}>
                <TableCell>{t.description}</TableCell>
                <TableCell>{t.status}</TableCell>
                <TableCell>{t.transactionType}</TableCell>
                <TableCell>{new Date(t.createdAt).toLocaleString()}</TableCell>
                {(role === "Admin" || role === "Operator") && (
                  <TableCell>
                    {t.status === "Posted" && (
                      <Button
                        variant="outlined"
                        color="error"
                        size="small"
                        onClick={() => reverseTransaction(t.transactionId)}
                      >
                        Reverse
                      </Button>
                    )}
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Container>
  );
}