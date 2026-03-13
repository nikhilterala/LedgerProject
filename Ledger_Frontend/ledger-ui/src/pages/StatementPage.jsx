import { useEffect, useState } from "react";
import { Container, Typography, Table, TableRow, TableCell, TableHead, TableBody } from "@mui/material";
import { useParams } from "react-router-dom";
import api from "../api/apiClient";

export default function StatementPage() {

  const [entries, setEntries] = useState([]);
  const { accountId } = useParams();

  useEffect(() => {
    loadStatement();
  }, []);

  const loadStatement = async () => {
    const res = await api.get(`/ledger/statement/${accountId}`);
    setEntries(res.data);
  };

  return (
    <Container>
      <Typography variant="h4" sx={{ mt: 3 }}>
        Account Statement
      </Typography>

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
    </Container>
  );
}