import { Drawer, List, ListItemButton, ListItemText, Toolbar, AppBar, Typography, Box } from "@mui/material";
import { useNavigate } from "react-router-dom";
import {jwtDecode} from "jwt-decode";

const drawerWidth = 220;

export default function Layout({ children }) {

  const navigate = useNavigate();

  const token = localStorage.getItem("token");
  let role = "";

  if (token) {
    const decoded = jwtDecode(token);
    role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  }

  return (
    <Box sx={{ display: "flex" }}>

      <AppBar position="fixed">
        <Toolbar>
          <Typography variant="h6">
            Ledger System
          </Typography>
        </Toolbar>
      </AppBar>

      <Drawer
        variant="permanent"
        sx={{
          width: drawerWidth,
          flexShrink: 0,
          [`& .MuiDrawer-paper`]: { width: drawerWidth, boxSizing: "border-box" }
        }}
      >
        <Toolbar />


            <List>

            <ListItemButton onClick={() => navigate("/")}>
            <ListItemText primary="Dashboard"/>
            </ListItemButton>

            {role !== "User" && (
              <>
                <ListItemButton onClick={() => navigate("/accounts")}>
                <ListItemText primary="Accounts"/>
                </ListItemButton>

                <ListItemButton onClick={() => navigate("/transactions")}>
                <ListItemText primary="Transactions"/>
                </ListItemButton>

                <ListItemButton onClick={() => navigate("/lookup-statement")}>
                <ListItemText primary="Lookup Statement"/>
                </ListItemButton>
              </>
            )}

            {role === "User" && (
              <ListItemButton onClick={() => navigate("/my-statement")}>
              <ListItemText primary="My Statement"/>
              </ListItemButton>
            )}

            <ListItemButton onClick={() => navigate("/create-transaction")}>
            <ListItemText primary="Create Transaction"/>
            </ListItemButton>

            {role === "Admin" && (
              <>
                <ListItemButton onClick={() => navigate("/create-adjustment")}>
                <ListItemText primary="Create Adjustment"/>
                </ListItemButton>

                <ListItemButton onClick={() => navigate("/admin/users")}>
                <ListItemText primary="Create User"/>
                </ListItemButton>
              </>
            )}

            <ListItemButton
            onClick={()=>{
            localStorage.removeItem("token")
            window.location.href="/login"
            }}
            >
            <ListItemText primary="Logout"/>
            </ListItemButton>

            </List>

      </Drawer>

      <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
        <Toolbar />
        {children}
      </Box>

    </Box>
  );
}