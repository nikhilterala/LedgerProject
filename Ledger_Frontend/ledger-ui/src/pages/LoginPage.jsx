import { useState } from "react";
import { TextField, Button, Container, Typography } from "@mui/material";
import api from "../api/apiClient";

export default function LoginPage() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  const login = async () => {
    const res = await api.post("/auth/login", null, {
      params: { username, password }
    });

    localStorage.setItem("token", res.data.token);
    window.location.href = "/";
  };

  return (
    <Container maxWidth="sm">
      <Typography variant="h4" sx={{ mt: 4 }}>Ledger Login</Typography>

      <TextField
        fullWidth
        label="Username"
        sx={{ mt: 3 }}
        value={username}
        onChange={(e) => setUsername(e.target.value)}
      />

      <TextField
        fullWidth
        label="Password"
        type="password"
        sx={{ mt: 2 }}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />

      <Button variant="contained" sx={{ mt: 3 }} onClick={login}>
        Login
      </Button>
    </Container>
  );
}