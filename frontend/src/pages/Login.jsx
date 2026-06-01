import React, { useState } from 'react';
import { Container, Card, CardContent, Typography, TextField, Button, Box, Alert, CircularProgress, Link } from '@mui/material';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { loginStart, loginSuccess, loginFailure } from '../store/authSlice';
import api from '../services/api';

const Login = () => {
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { loading, error } = useSelector((state) => state.auth);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!phone || !password) return;

    dispatch(loginStart());

    try {
      // API expects standard form-data for OAuth2PasswordRequestForm
      const formData = new URLSearchParams();
      formData.append('username', phone); // username matches the phone field
      formData.append('password', password);

      const response = await api.post('/api/auth/login', formData, {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
      });

      dispatch(loginSuccess(response.data));
      
      // Redirect based on role
      if (response.data.user.role === 'admin') {
        navigate('/admin');
      } else {
        navigate('/dashboard');
      }
    } catch (err) {
      const errMsg = err.response?.data?.detail || 'Incorrect phone number or password. Please try again.';
      dispatch(loginFailure(errMsg));
    }
  };

  return (
    <Box sx={{ position: 'relative', overflow: 'hidden', minHeight: '100vh', display: 'flex', alignItems: 'center', py: 6 }}>
      <div className="wedding-bg-pattern" />
      <Container maxWidth="sm" sx={{ position: 'relative', zIndex: 1 }}>
        <Typography 
          component={RouterLink} 
          to="/" 
          variant="h3" 
          align="center" 
          className="gold-gradient-text" 
          sx={{ display: 'block', textDecoration: 'none', fontWeight: 800, mb: 4 }}
        >
          Shagun
        </Typography>
        <Card sx={{ p: { xs: 2, md: 4 } }}>
          <CardContent>
            <Typography variant="h4" gutterBottom align="center" color="secondary.main" sx={{ mb: 4 }}>
              Host Login
            </Typography>

            {error && (
              <Alert severity="error" sx={{ mb: 3 }}>
                {error}
              </Alert>
            )}

            <form onSubmit={handleSubmit}>
              <TextField
                fullWidth
                label="Phone Number"
                placeholder="e.g. +919876543210"
                variant="outlined"
                margin="normal"
                required
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
              />
              <TextField
                fullWidth
                label="Password"
                type="password"
                variant="outlined"
                margin="normal"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
              <Button
                fullWidth
                type="submit"
                variant="contained"
                size="large"
                color="primary"
                disabled={loading}
                sx={{ mt: 3, py: 1.5 }}
              >
                {loading ? <CircularProgress size={24} color="inherit" /> : 'Log In'}
              </Button>
            </form>
            <Box sx={{ mt: 3, textAlign: 'center' }}>
              <Typography variant="body2" color="text.secondary">
                Don't have an event registered?{' '}
                <Link component={RouterLink} to="/register" color="primary.main" sx={{ fontWeight: 600 }}>
                  Create Wedding
                </Link>
              </Typography>
            </Box>
          </CardContent>
        </Card>
      </Container>
    </Box>
  );
};

export default Login;
