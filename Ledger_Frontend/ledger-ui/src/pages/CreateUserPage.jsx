import { useState, useEffect } from "react";
import {
  Container, Typography, TextField, Button,
  FormControl, InputLabel, Select, MenuItem,
  Table, TableBody, TableCell, TableHead, TableRow, Alert, Box
} from "@mui/material";
import api from "../api/apiClient";

export default function CreateUserPage() {

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState("");
  
  const [users, setUsers] = useState([]);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      const res = await api.get("/auth/users");
      setUsers(res.data);
    } catch (err) {
      setError("Failed to load users: " + err.message);
    }
  };

  const createUser = async () => {
    const payload = {
      username: username,
      password: password,
      roles: [role]
    };

    try {
      await api.post("/auth/create-user", payload);
      alert("User created successfully");
      setUsername("");
      setPassword("");
      setRole("");
      loadUsers(); // refresh the list
    } catch (err) {
      alert("Failed to create user: " + (err.response?.data || err.message));
    }
  };

  const deleteUser = async (usernameToDelete) => {
    if(!window.confirm(`Are you sure you want to delete user ${usernameToDelete}?`)) return;

    try {
      await api.delete(`/auth/users/${usernameToDelete}`);
      alert("User deleted successfully.");
      loadUsers(); // refresh
    } catch (err) {
      alert("Failed to delete user: " + (err.response?.data || err.message));
    }
  };

  return (
    <Container>

      <Typography variant="h4" sx={{ mt: 3, mb: 2 }}>
        Manage Users
      </Typography>

      <Box sx={{ border: '1px solid #ccc', p: 3, mb: 5, borderRadius: 2 }}>
        <Typography variant="h6">Create New User</Typography>
        <Typography variant="body2" color="info.main" sx={{ mb: 2 }}>
          Note: Creating a user with the 'User' role will automatically initialize a Ledger Account for them.
        </Typography>

        <TextField
          label="Username"
          fullWidth
          sx={{ mt: 2 }}
          value={username}
          onChange={(e) => setUsername(e.target.value)}
        />

        <TextField
          label="Password"
          type="password"
          fullWidth
          sx={{ mt: 2 }}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />

        <FormControl fullWidth sx={{ mt: 2 }}>
          <InputLabel>Role</InputLabel>
          <Select
            value={role}
            label="Role"
            onChange={(e) => setRole(e.target.value)}
          >
            <MenuItem value="Admin">Admin</MenuItem>
            <MenuItem value="Operator">Operator</MenuItem>
            <MenuItem value="User">User</MenuItem>
          </Select>
        </FormControl>

        <Button
          variant="contained"
          sx={{ mt: 3 }}
          onClick={createUser}
          disabled={!username || !password || !role}
        >
          Create User
        </Button>
      </Box>

      {/* ACTIVE USERS SECTION */}
      <Typography variant="h5" sx={{ mb: 2 }}>
        Active Users
      </Typography>
      
      {error && <Alert severity="error">{error}</Alert>}

      <Table>
        <TableHead>
          <TableRow>
            <TableCell>ID</TableCell>
            <TableCell>Username</TableCell>
            <TableCell>Roles</TableCell>
            <TableCell>Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {users.map(u => (
            <TableRow key={u.userId}>
              <TableCell>{u.userId}</TableCell>
              <TableCell>{u.username}</TableCell>
              <TableCell>{u.roles.join(', ')}</TableCell>
              <TableCell>
                <Button 
                  variant="outlined" 
                  color="error" 
                  size="small"
                  onClick={() => deleteUser(u.username)}
                >
                  Delete
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

    </Container>
  );
}