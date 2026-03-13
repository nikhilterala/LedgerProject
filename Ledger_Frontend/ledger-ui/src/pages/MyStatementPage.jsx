import { useEffect, useState } from "react";
import { Container, Typography, Table, TableRow, TableCell, TableHead, TableBody, Alert } from "@mui/material";
import api from "../api/apiClient";

export default function MyStatementPage() {
  const [entries, setEntries] = useState([]);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadStatement();
  }, []);

  const loadStatement = async () => {
    try {
      const res = await api.get("/ledger/my-statement");
      setEntries(res.data);
      setError(null);
    } catch (err) {
      setError(err.response?.data || "Unable to load statement.");
    }
  };

  return (
    <Container>
      <Typography variant="h4" sx={{ mt: 3, mb: 3 }}>
        My Account Statement
      </Typography>

      {error ? (
        <Alert severity="error">{error}</Alert>
      ) : (
        <Table sx={{ mt: 3 }}>
          <TableHead>
            <TableRow>
              <TableCell>Date</TableCell>
              <TableCell>Description</TableCell>
              <TableCell>Type</TableCell>
              <TableCell>Amount</TableCell>
              <TableCell>Running Balance</TableCell>
            </TableRow>
          </TableHead>

          <TableBody>
            {entries.map(e => (
              <TableRow key={e.transactionId}>
                <TableCell>{new Date(e.date).toLocaleString()}</TableCell>
                <TableCell>{e.description}</TableCell>
                <TableCell>{e.amount > 0 ? "Credit" : "Debit"}</TableCell>
                <TableCell>{e.amount}</TableCell>
                <TableCell>{e.runningBalance}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Container>
  );
}
