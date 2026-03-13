import { useState } from "react";
import { Container, Typography, TextField, Button, Box } from "@mui/material";
import { useNavigate } from "react-router-dom";

export default function StatementLookupPage() {
  const [accountId, setAccountId] = useState("");
  const navigate = useNavigate();

  const handleLookup = () => {
    if (accountId.trim()) {
      navigate(`/accounts/${accountId.trim()}/statement`);
    }
  };

  return (
    <Container maxWidth="sm">
      <Typography variant="h4" sx={{ mt: 3, mb: 3 }}>
        Lookup Statement
      </Typography>

      <Typography variant="body1" sx={{ mb: 3 }} color="text.secondary">
        Enter an Account ID (GUID) to directly view its ledger statement.
      </Typography>

      <Box sx={{ display: "flex", gap: 2 }}>
        <TextField
          fullWidth
          label="Account ID"
          variant="outlined"
          value={accountId}
          onChange={(e) => setAccountId(e.target.value)}
          placeholder="e.g. 123e4567-e89b-12d3-a456-426614174000"
        />
        <Button 
          variant="contained" 
          onClick={handleLookup}
          disabled={!accountId.trim()}
        >
          View
        </Button>
      </Box>
    </Container>
  );
}
