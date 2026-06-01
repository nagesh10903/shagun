import React from 'react';
import { Container, Typography, Button, Box, Grid, Card, CardContent, Link } from '@mui/material';
import { Favorite, AddCard, PersonAdd, Share, AccountBalanceWallet } from '@mui/icons-material';
import { Link as RouterLink } from 'react-router-dom';

const LandingPage = () => {
  return (
    <Box sx={{ position: 'relative', overflow: 'hidden', minHeight: '100vh', pb: 8 }}>
      <div className="wedding-bg-pattern" />

      {/* Navigation Header */}
      <Container maxWidth="lg" sx={{ py: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'center', position: 'relative', zIndex: 1 }}>
        <Typography variant="h4" className="gold-gradient-text" sx={{ fontWeight: 800 }}>
          Shagun
        </Typography>
        <Box>
          <Button component={RouterLink} to="/login" variant="text" sx={{ mr: 2, color: 'primary.main' }}>
            Login
          </Button>
          <Button component={RouterLink} to="/register" variant="contained" color="primary">
            Create Wedding
          </Button>
        </Box>
      </Container>

      {/* Hero Section */}
      <Container maxWidth="md" sx={{ textAlign: 'center', py: { xs: 8, md: 12 }, position: 'relative', zIndex: 1 }}>
        <Typography variant="h2" component="h1" gutterBottom sx={{ fontWeight: 800, color: 'secondary.main', mb: 3 }}>
          Celebrate Love, <br />
          <span className="gold-gradient-text">Share the Joy.</span>
        </Typography>
        <Typography variant="h6" color="text.secondary" sx={{ maxWidth: '600px', mx: 'auto', mb: 5, fontWeight: 400 }}>
          Shagun is a wedding gift contribution platform. Create your wedding event, list your dream gifts, and let guests contribute cash securely. No overfunding, fully automated.
        </Typography>
        <Box>
          <Button component={RouterLink} to="/register" variant="contained" size="large" sx={{ mr: 2, px: 4, py: 1.5, fontSize: '1.1rem' }}>
            Get Started
          </Button>
          <Button href="#how-it-works" variant="outlined" color="primary" size="large" sx={{ px: 4, py: 1.5, fontSize: '1.1rem' }}>
            How it Works
          </Button>
        </Box>
      </Container>

      {/* Features Grid */}
      <Container id="how-it-works" maxWidth="lg" sx={{ py: 8, position: 'relative', zIndex: 1 }}>
        <Typography variant="h3" align="center" gutterBottom sx={{ mb: 6, color: 'secondary.main' }}>
          How it Works
        </Typography>

        <Grid container spacing={4}>
          <Grid item xs={12} md={3}>
            <Card className="hover-scale" sx={{ height: '100%', textAlign: 'center', p: 3 }}>
              <CardContent>
                <Box sx={{ color: 'primary.main', mb: 2 }}>
                  <Favorite sx={{ fontSize: 48 }} />
                </Box>
                <Typography variant="h5" gutterBottom sx={{ mb: 1 }}>
                  1. Create Event
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Register, enter wedding details, upload a beautiful cover photo and write a description.
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card className="hover-scale" sx={{ height: '100%', textAlign: 'center', p: 3 }}>
              <CardContent>
                <Box sx={{ color: 'primary.main', mb: 2 }}>
                  <AddCard sx={{ fontSize: 48 }} />
                </Box>
                <Typography variant="h5" gutterBottom sx={{ mb: 1 }}>
                  2. Add Dream Gifts
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Add desired gifts like honeymoons, appliances, or home goods along with estimated costs.
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card className="hover-scale" sx={{ height: '100%', textAlign: 'center', p: 3 }}>
              <CardContent>
                <Box sx={{ color: 'primary.main', mb: 2 }}>
                  <PersonAdd sx={{ fontSize: 48 }} />
                </Box>
                <Typography variant="h5" gutterBottom sx={{ mb: 1 }}>
                  3. Invite Guests
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Upload an invitee CSV or excel. Automatically generate unique, secure invitation links.
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={3}>
            <Card className="hover-scale" sx={{ height: '100%', textAlign: 'center', p: 3 }}>
              <CardContent>
                <Box sx={{ color: 'primary.main', mb: 2 }}>
                  <AccountBalanceWallet sx={{ fontSize: 48 }} />
                </Box>
                <Typography variant="h5" gutterBottom sx={{ mb: 1 }}>
                  4. Recieve Funds
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Guests contribute partial or full amounts. Contributions lock once the target cost is hit.
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </Container>
    </Box>
  );
};

export default LandingPage;
