import { useEffect, useState } from "react";
import {
  Container, Typography, TextField, Button,
  FormControl, InputLabel, Select, MenuItem, Box
} from "@mui/material";
import api from "../api/apiClient";

export default function CreateAdjustmentPage() {

  const [description, setDescription] = useState("");
  const [selectedAccount, setSelectedAccount] = useState("");
  const [amount, setAmount] = useState("");
  const [accounts, setAccounts] = useState([]);

  useEffect(() => {
    loadAccounts();
  }, []);

  const loadAccounts = async () => {
    try {
      const res = await api.get("/ledger/accounts");
      setAccounts(res.data);
    } catch {
      setAccounts([]);
    }
  };

  const createAdjustment = async () => {
    const payload = {
      description: description,
      idempotencyKey: crypto.randomUUID(),
      entries: [
        {
          accountId: selectedAccount,
          amount: parseFloat(amount)
        }
      ]
    };

    try {
      await api.post("/ledger/adjustment", payload);
      alert("Adjustment created successfully");
      setDescription("");
      setSelectedAccount("");
      setAmount("");
    } catch (err) {
      alert("Failed to create adjustment: " + (err.response?.data || err.message));
    }
  };

  return (
    <Container maxWidth="sm">
      <Typography variant="h4" sx={{ mt: 3, mb: 3 }}>
        Create Manual Adjustment
      </Typography>

      <Typography variant="body2" color="warning.main" sx={{ mb: 3 }}>
        Warning: Adjustments explicitly alter account balances without double-entry clearing. 
        Only use for system corrections.
      </Typography>

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
        <TextField
          label="Adjustment Reason / Description"
          fullWidth
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />

        <FormControl fullWidth>
          <InputLabel>Target Account</InputLabel>
          <Select
            value={selectedAccount}
            label="Target Account"
            onChange={(e) => setSelectedAccount(e.target.value)}
          >
            {accounts.map(a => (
              <MenuItem key={a.accountId} value={a.accountId}>
                {a.accountName}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <TextField
          label="Amount (Positive to Credit, Negative to Debit)"
          type="number"
          fullWidth
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
        />

        <Button
          variant="contained"
          color="warning"
          onClick={createAdjustment}
          disabled={!description || !selectedAccount || !amount}
        >
          Submit Adjustment
        </Button>
      </Box>
    </Container>
  );
}
