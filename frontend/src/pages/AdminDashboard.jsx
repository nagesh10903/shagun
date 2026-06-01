import React, { useState, useEffect } from 'react';
import {
  Container, Typography, Box, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, Paper, Tabs, Tab,
  Button, Select, MenuItem, Alert, CircularProgress
} from '@mui/material';
import { Logout } from '@mui/icons-material';
import { useDispatch } from 'react-redux';
import { logout } from '../store/authSlice';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';

const AdminDashboard = () => {
  const dispatch = useDispatch();
  const navigate = useNavigate();

  // States
  const [activeTab, setActiveTab] = useState(0);
  const [users, setUsers] = useState([]);
  const [events, setEvents] = useState([]);
  const [payments, setPayments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchAdminData = async () => {
    setLoading(true);
    setError(null);
    try {
      if (activeTab === 0) {
        const res = await api.get('/api/admin/users');
        setUsers(res.data);
      } else if (activeTab === 1) {
        const res = await api.get('/api/admin/events');
        setEvents(res.data);
      } else if (activeTab === 2) {
        const res = await api.get('/api/admin/payments');
        setPayments(res.data);
      }
    } catch (err) {
      setError('Unauthorized access or network error. Verify admin session.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAdminData();
  }, [activeTab]);

  const handleRoleChange = async (userId, newRole) => {
    try {
      await api.put(`/api/admin/users/${userId}/role?role=${newRole}`);
      alert('Role updated successfully!');
      fetchAdminData();
    } catch (err) {
      alert('Failed to update role.');
    }
  };

  const handleLogout = () => {
    dispatch(logout());
    navigate('/login');
  };

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
        <Typography variant="h3" className="gold-gradient-text" sx={{ fontWeight: 800 }}>
          Admin Console
        </Typography>
        <Button variant="outlined" color="secondary" startIcon={<LogOut />} onClick={handleLogout}>
          Logout
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}

      <Tabs value={activeTab} onChange={(e, val) => setActiveTab(val)} sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tab label="Manage Users" />
        <Tab label="Monitor Events" />
        <Tab label="View Payments Log" />
      </Tabs>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress color="primary" />
        </Box>
      ) : (
        <TableContainer component={Paper}>
          {activeTab === 0 && (
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>ID</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell>Phone</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell>Role</TableCell>
                  <TableCell>Registered At</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {users.map((u) => (
                  <TableRow key={u.id}>
                    <TableCell>{u.id}</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>{u.name}</TableCell>
                    <TableCell>{u.phone}</TableCell>
                    <TableCell>{u.email || 'N/A'}</TableCell>
                    <TableCell>
                      <Select
                        size="small"
                        value={u.role}
                        onChange={(e) => handleRoleChange(u.id, e.target.value)}
                      >
                        <MenuItem value="host">Host</MenuItem>
                        <MenuItem value="admin">Admin</MenuItem>
                      </Select>
                    </TableCell>
                    <TableCell>{new Date(u.created_at).toLocaleDateString()}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          {activeTab === 1 && (
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Event ID</TableCell>
                  <TableCell>Event Name</TableCell>
                  <TableCell>Groom & Bride</TableCell>
                  <TableCell>Date</TableCell>
                  <TableCell>Venue</TableCell>
                  <TableCell>Status</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {events.map((ev) => (
                  <TableRow key={ev.id}>
                    <TableCell>{ev.id}</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>{ev.event_name}</TableCell>
                    <TableCell>{ev.groom_name} & {ev.bride_name}</TableCell>
                    <TableCell>{new Date(ev.event_date).toLocaleDateString()}</TableCell>
                    <TableCell>{ev.venue}</TableCell>
                    <TableCell>{ev.status}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          {activeTab === 2 && (
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Tx ID</TableCell>
                  <TableCell>Gateway</TableCell>
                  <TableCell>Order ID</TableCell>
                  <TableCell>Payment ID</TableCell>
                  <TableCell>Amount</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Created At</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {payments.map((p) => (
                  <TableRow key={p.id}>
                    <TableCell>{p.id}</TableCell>
                    <TableCell>{p.gateway}</TableCell>
                    <TableCell>{p.gateway_order_id}</TableCell>
                    <TableCell>{p.gateway_payment_id || 'N/A'}</TableCell>
                    <TableCell sx={{ color: 'success.main', fontWeight: 600 }}>₹{parseFloat(p.amount).toLocaleString()}</TableCell>
                    <TableCell>{p.status}</TableCell>
                    <TableCell>{new Date(p.created_at).toLocaleDateString()}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </TableContainer>
      )}
    </Container>
  );
};

export default AdminDashboard;
