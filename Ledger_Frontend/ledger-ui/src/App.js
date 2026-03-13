import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import LoginPage from "./pages/LoginPage";
import DashboardPage from "./pages/DashboardPage";
import AccountsPage from "./pages/AccountsPage";
import TransactionsPage from "./pages/TransactionsPage";
import CreateTransactionPage from "./pages/CreateTransactionPage";
import CreateUserPage from "./pages/CreateUserPage";
import StatementPage from "./pages/StatementPage";
import StatementLookupPage from "./pages/StatementLookupPage";
import MyStatementPage from "./pages/MyStatementPage";
import CreateAdjustmentPage from "./pages/CreateAdjustmentPage";
import Layout from "./components/Layout";

function PrivateRoute({ children }) {
  const token = localStorage.getItem("token");

  if (!token) {
    return <Navigate to="/login" />;
  }

  return children;
}

function App() {
  return (
    <BrowserRouter>
      <Routes>

        <Route path="/login" element={<LoginPage />} />

        <Route path="/" element={<PrivateRoute><Layout><DashboardPage /></Layout></PrivateRoute>} />

        <Route path="/accounts" element={<PrivateRoute><Layout><AccountsPage /></Layout></PrivateRoute>} />

        <Route path="/accounts/:accountId/statement" element={<PrivateRoute><Layout><StatementPage /></Layout></PrivateRoute>} />
        
        <Route path="/lookup-statement" element={<PrivateRoute><Layout><StatementLookupPage /></Layout></PrivateRoute>} />

        <Route path="/my-statement" element={<PrivateRoute><Layout><MyStatementPage /></Layout></PrivateRoute>} />

        <Route path="/transactions" element={<PrivateRoute><Layout><TransactionsPage /></Layout></PrivateRoute>} />

        <Route path="/create-transaction" element={<PrivateRoute><Layout><CreateTransactionPage /></Layout></PrivateRoute>} />

        <Route path="/create-adjustment" element={<PrivateRoute><Layout><CreateAdjustmentPage /></Layout></PrivateRoute>} />

        <Route path="/admin/users" element={<PrivateRoute><Layout><CreateUserPage /></Layout></PrivateRoute>} />

      </Routes>
    </BrowserRouter>
  );
}

export default App;