import React, { useState } from 'react';
import { Container, Card, CardContent, Typography, TextField, Button, Box, Alert, CircularProgress, Link } from '@mui/material';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import api from '../services/api';

const Register = () => {
  const [name, setName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!name || !phone || !password) return;

    setLoading(true);
    setError(null);

    try {
      await api.post('/api/auth/register', {
        name,
        phone,
        email: email || null,
        password,
        role: 'host',
      });

      setSuccess(true);
      setLoading(false);
      setTimeout(() => {
        navigate('/login');
      }, 2000);
    } catch (err) {
      setLoading(false);
      setError(err.response?.data?.detail || 'Registration failed. Check if phone is already registered.');
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
            <Typography variant="h4" gutterBottom align="center" color="secondary.main" sx={{ mb: 2 }}>
              Host Registration
            </Typography>
            <Typography variant="body2" align="center" color="text.secondary" sx={{ mb: 4 }}>
              Register as a host to set up wedding events and invite guests.
            </Typography>

            {error && (
              <Alert severity="error" sx={{ mb: 3 }}>
                {error}
              </Alert>
            )}

            {success && (
              <Alert severity="success" sx={{ mb: 3 }}>
                Successfully registered! Redirecting to login...
              </Alert>
            )}

            <form onSubmit={handleSubmit}>
              <TextField
                fullWidth
                label="Full Name"
                variant="outlined"
                margin="normal"
                required
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
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
                label="Email Address (Optional)"
                type="email"
                variant="outlined"
                margin="normal"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
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
                disabled={loading || success}
                sx={{ mt: 3, py: 1.5 }}
              >
                {loading ? <CircularProgress size={24} color="inherit" /> : 'Register'}
              </Button>
            </form>
            <Box sx={{ mt: 3, textAlign: 'center' }}>
              <Typography variant="body2" color="text.secondary">
                Already registered?{' '}
                <Link component={RouterLink} to="/login" color="primary.main" sx={{ fontWeight: 600 }}>
                  Log In
                </Link>
              </Typography>
            </Box>
          </CardContent>
        </Card>
      </Container>
    </Box>
  );
};

export default Register;
