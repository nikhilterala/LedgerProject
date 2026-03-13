import { useEffect, useState } from "react";
import { Container, Typography, Button, Card, CardContent } from "@mui/material";
import { jwtDecode } from "jwt-decode";
import api from "../api/apiClient";

export default function DashboardPage() {

  const [frozenCount, setFrozenCount] = useState(0);
  const [userAccount, setUserAccount] = useState(null);

  const token = localStorage.getItem("token");
  let role = "";
  let username = "";
  if (token) {
    try {
      const decoded = jwtDecode(token);
      role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
      username = decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] ||
        decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/name"] ||
        decoded["unique_name"] ||
        decoded["sub"] ||
        decoded["name"];
    } catch (err) {
      // Decode failed
    }
  }

  useEffect(() => {
    if (role === "Admin") {
      loadAdminData();
    } else if (role === "User") {
      loadUserData();
    }
  }, [username]);

  const loadAdminData = async () => {
    try {
      const res = await api.get("/ledger/system/frozen-accounts");
      setFrozenCount(res.data.length);
    } catch {
      setFrozenCount(0);
    }
  };

  const loadUserData = async () => {
    if (!username) return;

    try {
      const res = await api.get("/ledger/accounts");
      const found = res.data.find(a => a.accountName === username);
      if (found) {
        const balRes = await api.get(`/ledger/balance/${found.accountId}`);
        setUserAccount(balRes.data);
      }
    } catch (err) {
      // Load failed
    }
  };

  const runReconciliation = async () => {
    await api.post("/ledger/reconcile");
    alert("Reconciliation completed");
    loadAdminData();
  };

  return (
    <Container>

      <Typography variant="h4" sx={{ mb: 3 }}>
        Dashboard
      </Typography>

      {role === "Admin" && (
        <>
          <Card sx={{ mb: 3, maxWidth: 400 }}>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>System Health</Typography>
              <Typography variant="h6">Frozen Accounts</Typography>
              <Typography variant="h3" color="error.main">{frozenCount}</Typography>
            </CardContent>
          </Card>

          <Button variant="contained" onClick={runReconciliation}>
            Run System Reconciliation
          </Button>
        </>
      )}

      {role === "User" && (
        <Card sx={{ mb: 3, maxWidth: 400, background: 'linear-gradient(45deg, #1976d2 30%, #21cbf3 90%)', color: 'white' }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>Your Balance</Typography>
            {userAccount ? (
              <>
                <Typography variant="h3">${userAccount.balance.toLocaleString(undefined, { minimumFractionDigits: 2 })}</Typography>
                <Typography variant="body2" sx={{ mt: 1, opacity: 0.8 }}>Account: {userAccount.accountName}</Typography>
              </>
            ) : (
              <Typography variant="h6">Loading balance...</Typography>
            )}
          </CardContent>
        </Card>
      )}

      {(role !== "Admin" && role !== "User") && (
        <Typography variant="body1" color="text.secondary">
          Welcome to the Ledger System. Use the sidebar to navigate through your authorized operations.
        </Typography>
      )}

    </Container>
  );
}