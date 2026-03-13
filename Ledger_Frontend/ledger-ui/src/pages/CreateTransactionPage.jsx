import { useEffect, useState } from "react";
import {
  Container, Typography, TextField, Button,
  FormControl, InputLabel, Select, MenuItem, Alert
} from "@mui/material";
import api from "../api/apiClient";
import { jwtDecode } from "jwt-decode";

export default function CreateTransactionPage() {

  const [description, setDescription] = useState("");
  const [account1, setAccount1] = useState("");
  const [account2, setAccount2] = useState("");
  const [amount, setAmount] = useState("");
  const [accounts, setAccounts] = useState([]);
  const [error, setError] = useState(null);

  const token = localStorage.getItem("token");
  let role = "";
  let username = "";
  if (token) {
    const decoded = jwtDecode(token);
    role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
    username = decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] ||
               decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/name"] ||
               decoded["unique_name"] ||
               decoded["sub"] ||
               decoded["name"];
  }

  useEffect(() => {
    loadAccounts();
  }, [username]);

  const loadAccounts = async () => {
    try {
      const res = await api.get("/ledger/accounts");
      const allAccounts = res.data;
      setAccounts(allAccounts);

      if (role === "User" && username) {
        const userAccount = allAccounts.find(a => a.accountName === username);
        if (userAccount) {
          setAccount1(userAccount.accountId);
        }
      }
    } catch (err) {
      setError("Failed to load accounts.");
    }
  };

  const createTransaction = async () => {
    const payload = {
      description: description,
      idempotencyKey: crypto.randomUUID(),
      entries: [
        {
          accountId: account1,
          amount: -Math.abs(parseFloat(amount)),
          entryType: "Debit",
          narration: description
        },
        {
          accountId: account2,
          amount: Math.abs(parseFloat(amount)),
          entryType: "Credit",
          narration: description
        }
      ]
    };

    try {
      await api.post("/ledger/create", payload);
      alert("Transaction created successfully");
      setDescription("");
      setAmount("");
      if (role !== "User") setAccount1("");
      setAccount2("");
    } catch (err) {
      alert("Failed to create transaction: " + (err.response?.data || err.message));
    }
  };

  const receiverAccounts = accounts.filter(a => a.accountId !== account1);

  return (
    <Container maxWidth="sm">
      <Typography variant="h4" sx={{ mt: 3, mb: 3 }}>
        Create Transaction
      </Typography>

      {error && <Alert severity="error">{error}</Alert>}

      <TextField
        label="Description"
        fullWidth
        sx={{ mt: 2 }}
        value={description}
        onChange={(e) => setDescription(e.target.value)}
      />

      <FormControl fullWidth sx={{ mt: 2 }}>
        {role === "User" ? (
          <TextField
            label="From Account (Sender)"
            fullWidth
            value={username || "Loading..."}
            slotProps={{ input: { readOnly: true } }}
            helperText="Regular users are restricted to their own account."
          />
        ) : (
          <>
            <InputLabel>From Account (Sender)</InputLabel>
            <Select
              value={account1}
              label="From Account (Sender)"
              onChange={(e) => setAccount1(e.target.value)}
            >
              {accounts.map(a => (
                <MenuItem key={a.accountId} value={a.accountId}>
                  {a.accountName}
                </MenuItem>
              ))}
            </Select>
          </>
        )}
      </FormControl>

      <FormControl fullWidth sx={{ mt: 2 }}>
        <InputLabel>To Account (Receiver)</InputLabel>
        <Select
          value={account2}
          label="To Account (Receiver)"
          onChange={(e) => setAccount2(e.target.value)}
        >
          {receiverAccounts.map(a => (
            <MenuItem key={a.accountId} value={a.accountId}>
              {a.accountName}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <TextField
        label="Amount"
        type="number"
        fullWidth
        sx={{ mt: 2 }}
        value={amount}
        onChange={(e) => setAmount(e.target.value)}
      />

      <Button
        variant="contained"
        fullWidth
        sx={{ mt: 4, py: 1.5 }}
        onClick={createTransaction}
        disabled={!description || !account1 || !account2 || !amount}
      >
        Submit Transaction
      </Button>
    </Container>
  );
}