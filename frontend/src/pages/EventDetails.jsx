import React, { useState, useEffect } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import {
  Container, Typography, Box, Grid, Card, CardContent, Button,
  Dialog, DialogTitle, DialogContent, DialogActions, TextField,
  LinearProgress, Stack, FormControlLabel, Checkbox, Alert, CircularProgress,
  Paper, InputAdornment, Avatar, Divider
} from '@mui/material';
import { Favorite, LocationOn, CalendarToday, HeartBroken, CardGiftcard } from '@mui/icons-material';
import api from '../services/api';

const EventDetails = () => {
  const { token } = useParams(); // URL path can be /invite/:token
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  // If page is accessed via `/events/:eventId` or search parameters
  const eventId = searchParams.get('event_id');

  // States
  const [invitee, setInvitee] = useState(null);
  const [event, setEvent] = useState(null);
  const [gifts, setGifts] = useState([]);
  const [publicFeed, setPublicFeed] = useState([]);
  const [isMockPayments, setIsMockPayments] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Contribution Form Modal State
  const [openContribModal, setOpenContribModal] = useState(false);
  const [selectedGift, setSelectedGift] = useState(null);
  const [contribAmount, setContribAmount] = useState('');
  const [anonymous, setAnonymous] = useState(false);
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [successMessage, setSuccessMessage] = useState(null);

  // Load Razorpay JS SDK dynamically
  const loadRazorpayScript = () => {
    return new Promise((resolve) => {
      if (window.Razorpay) {
        resolve(true);
        return;
      }
      const script = document.createElement('script');
      script.src = 'https://checkout.razorpay.com/v1/checkout.js';
      script.async = true;
      script.onload = () => resolve(true);
      script.onerror = () => resolve(false);
      document.body.appendChild(script);
    });
  };

  const fetchEventData = async () => {
    setLoading(true);
    setError(null);
    try {
      let activeEventId = eventId;

      // 1. Resolve token if provided
      if (token) {
        try {
          const inviteeRes = await api.get(`/api/events/event_id/invitees/token/${token}`);
          setInvitee(inviteeRes.data);
          activeEventId = inviteeRes.data.event_id;
        } catch (tokenErr) {
          setError("Invalid or expired invitation token.");
          setLoading(false);
          return;
        }
      }

      if (!activeEventId) {
        setError("Event ID or invitation token is required.");
        setLoading(false);
        return;
      }

      // 2. Fetch Event, Gifts and Public Contribution feed
      const [eventRes, giftsRes, feedRes] = await Promise.all([
        api.get(`/api/events/${activeEventId}`),
        api.get(`/api/events/${activeEventId}/gifts`),
        api.get(`/api/events/${activeEventId}/public-contributions`)
      ]);

      setEvent(eventRes.data);
      setGifts(giftsRes.data);
      setPublicFeed(feedRes.data);
    } catch (err) {
      setError("Failed to load wedding details. Please verify your invitation link.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // Fetch site and payment config in parallel
    const init = async () => {
      try {
        const cfg = await api.get('/api/payments/razorpay-config');
        setIsMockPayments(cfg.data?.use_mock ?? false);
      } catch (e) {
        // ignore
      }
      await fetchEventData();
    };
    init();
  }, [token, eventId]);

  const handleOpenContrib = (gift) => {
    const remaining = parseFloat(gift.estimated_cost) - parseFloat(gift.contributed_amount);
    if (remaining <= 0 || gift.status === 'FUNDED') return;
    setSelectedGift(gift);
    // Pre-suggest remaining balance or a typical value
    setContribAmount(Math.min(2000, remaining).toString());
    setOpenContribModal(true);
  };

  const handleContributeSubmit = async (e) => {
    e.preventDefault();
    if (!contribAmount || parseFloat(contribAmount) <= 0) return;

    const remaining = parseFloat(selectedGift.estimated_cost) - parseFloat(selectedGift.contributed_amount);
    if (parseFloat(contribAmount) > remaining) {
      alert(`Contribution exceeds remaining balance. Max allowed: ₹${remaining.toLocaleString()}`);
      return;
    }

    setPaymentLoading(true);

    try {
      // 1. Request contribution order creation
      const res = await api.post(`/api/gifts/${selectedGift.id}/contribute`, {
        amount: parseFloat(contribAmount),
        anonymous: anonymous,
        invitee_token: token || null
      });

      const { contribution, razorpay_order } = res.data;

      // 2. Load SDK and trigger checkout
      const sdkLoaded = await loadRazorpayScript();
      if (!sdkLoaded) {
        alert("Razorpay payment gateway failed to load. Please check your connection.");
        setPaymentLoading(false);
        return;
      }

      // If it's a mock workflow in development environment (no key returned)
      if (!razorpay_order.key_id) {
        // Automatically verify mock payment to make development seamless without payment keys
        await api.post('/api/payments/verify', {
          razorpay_order_id: razorpay_order.order_id,
          razorpay_payment_id: `pay_mock_${Date.now()}`,
          razorpay_signature: 'mock_signature_bypass',
          contribution_id: contribution.id
        });

        setSuccessMessage(`Thank you for contributing ₹${parseFloat(contribAmount).toLocaleString()} towards '${selectedGift.name}'!`);
        setOpenContribModal(false);
        fetchEventData();
      } else {
        // Live Razorpay options
        const options = {
          key: razorpay_order.key_id,
          amount: razorpay_order.amount,
          currency: razorpay_order.currency,
          name: event.event_name,
          description: `Gift Contribution for ${selectedGift.name}`,
          order_id: razorpay_order.order_id,
          handler: async (response) => {
            try {
              setPaymentLoading(true);
              await api.post('/api/payments/verify', {
                razorpay_order_id: response.razorpay_order_id,
                razorpay_payment_id: response.razorpay_payment_id,
                razorpay_signature: response.razorpay_signature,
                contribution_id: contribution.id
              });
              setSuccessMessage(`Thank you! Your contribution of ₹${parseFloat(contribAmount).toLocaleString()} was received successfully.`);
              setOpenContribModal(false);
              fetchEventData();
            } catch (err) {
              alert("Payment verification failed. Please contact support.");
            } finally {
              setPaymentLoading(false);
            }
          },
          prefill: {
            name: invitee ? invitee.name : '',
            contact: invitee ? invitee.phone : ''
          },
          theme: { color: "#b8860b" },
          modal: {
            ondismiss: () => {
              setPaymentLoading(false);
            }
          }
        };
        const rzp = new window.Razorpay(options);
        rzp.open();
      }
    } catch (err) {
      const serverDetail = err.response?.data?.detail;
      const serverRemaining = err.response?.data?.remaining;
      if (serverRemaining !== undefined && serverRemaining !== null) {
        alert(`${serverDetail}. Maximum allowed: ₹${parseFloat(serverRemaining).toLocaleString()}`);
      } else {
        alert(serverDetail || "Failed to process contribution. Try again.");
      }
    } finally {
      setPaymentLoading(false);
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <CircularProgress color="primary" />
      </Box>
    );
  }

  if (error) {
    return (
      <Container maxWidth="sm" sx={{ py: 8, textAlign: 'center' }}>
        <HeartBroken sx={{ fontSize: 70, color: 'error.main', mb: 3 }} />
        <Typography variant="h5" gutterBottom>{error}</Typography>
        <Typography variant="body2" color="text.secondary">
          Please check the URL for typos or ask the wedding host for a new link.
        </Typography>
      </Container>
    );
  }

  return (
    <Box sx={{ minHeight: '100vh', pb: 8, position: 'relative' }}>
      <div className="wedding-bg-pattern" />

      {/* Guest Personalized Greeting */}
      {invitee && (
        <Box sx={{ bg: '#fbf8f3', borderBottom: '1px solid rgba(212, 175, 55, 0.15)', py: 1.5, textAlign: 'center' }}>
          <Typography variant="body2" sx={{ fontWeight: 600, color: 'primary.main' }}>
            Welcome, {invitee.name}! You are cordially invited to celebrate with us.
          </Typography>
        </Box>
      )}

      {/* Wedding Cover Header */}
      <Box sx={{
        height: { xs: 260, md: 400 },
        backgroundImage: event.cover_photo_url ? `url(${event.cover_photo_url})` : 'linear-gradient(to right, #800020, #4a0012)',
        backgroundSize: 'cover',
        backgroundPosition: 'center',
        position: 'relative',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        color: 'white',
        textAlign: 'center',
        px: 2
      }}>
        {/* Overlay */}
        <Box sx={{ position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, bgcolor: 'rgba(0,0,0,0.4)', zIndex: 1 }} />

        <Box sx={{ position: 'relative', zIndex: 2 }}>
          <Typography variant="h2" sx={{ fontFamily: 'Playfair Display', fontWeight: 800, mb: 1, fontSize: { xs: '2rem', md: '3.5rem' } }}>
            {event.event_name}
          </Typography>
          <Typography variant="h5" sx={{ fontFamily: 'Playfair Display', opacity: 0.9 }}>
            {event.groom_name} & {event.bride_name}
          </Typography>
        </Box>
      </Box>

      <Container maxWidth="lg" sx={{ mt: -6, position: 'relative', zIndex: 3 }}>
        {successMessage && (
          <Alert severity="success" sx={{ mb: 3 }} onClose={() => setSuccessMessage(null)}>
            {successMessage}
          </Alert>
        )}
        {isMockPayments && (
          <Alert severity="info" sx={{ mb: 3 }}>
            Development mode: mock Razorpay payments active — payments will be auto-verified.
          </Alert>
        )}

        <Grid container spacing={4}>
          {/* Event Details Card */}
          <Grid item xs={12} md={4}>
            <Card sx={{ p: 1, mb: 4 }}>
              <CardContent>
                <Typography variant="h5" color="secondary.main" gutterBottom sx={{ fontFamily: 'Playfair Display', fontWeight: 700 }}>
                  Wedding Ceremony
                </Typography>
                <Divider sx={{ my: 2 }} />

                <Stack spacing={2.5}>
                  <Stack direction="row" spacing={2} alignItems="center">
                    <Avatar sx={{ bgcolor: 'primary.light' }}><CalendarToday fontSize="small" /></Avatar>
                    <Box>
                      <Typography variant="caption" color="text.secondary">Date</Typography>
                      <Typography variant="body1" sx={{ fontWeight: 600 }}>
                        {new Date(event.event_date).toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
                      </Typography>
                    </Box>
                  </Stack>

                  <Stack direction="row" spacing={2} alignItems="center">
                    <Avatar sx={{ bgcolor: 'primary.light' }}><LocationOn fontSize="small" /></Avatar>
                    <Box>
                      <Typography variant="caption" color="text.secondary">Venue</Typography>
                      <Typography variant="body1" sx={{ fontWeight: 600 }}>{event.venue}</Typography>
                    </Box>
                  </Stack>
                </Stack>

                {event.description && (
                  <>
                    <Divider sx={{ my: 3 }} />
                    <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic', lineHeight: 1.6 }}>
                      "{event.description}"
                    </Typography>
                  </>
                )}
              </CardContent>
            </Card>

            {/* Public contribution activity feed */}
            <Card>
              <CardContent>
                <Typography variant="h6" color="secondary.main" gutterBottom>
                  Recent Blessings & Contributions
                </Typography>
                <Divider sx={{ my: 1.5 }} />

                {publicFeed.length === 0 ? (
                  <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                    Be the first to leave a blessing!
                  </Typography>
                ) : (
                  <Stack spacing={2} sx={{ maxHeight: 300, overflowY: 'auto', pr: 1 }}>
                    {publicFeed.map((feed) => (
                      <Box key={feed.id} sx={{ p: 1.5, borderRadius: 2, bgcolor: '#fbfaf7', border: '1px solid #f1ece1' }}>
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                          {feed.display_name} <span style={{ fontWeight: 400, color: '#6b5e53' }}>contributed to</span> {feed.gift_item_name}
                        </Typography>
                        <Typography variant="subtitle2" color="primary.main" sx={{ fontWeight: 700 }}>
                          ₹{feed.amount.toLocaleString()}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {new Date(feed.created_at).toLocaleDateString()}
                        </Typography>
                      </Box>
                    ))}
                  </Stack>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* Gift List Grid */}
          <Grid item xs={12} md={8}>
            <Paper sx={{ p: 4, mb: 4 }}>
              <Typography variant="h4" color="secondary.main" gutterBottom sx={{ fontFamily: 'Playfair Display', fontWeight: 700 }}>
                Gift Contribution Registry
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>
                Help us start our new journey by contributing cash toward one or more of the items on our wishlist. Simply choose an item and input your preferred contribution amount.
              </Typography>

              {gifts.length === 0 ? (
                <Typography variant="body1" align="center" color="text.secondary" sx={{ py: 6 }}>
                  No items listed in the registry yet.
                </Typography>
              ) : (
                <Grid container spacing={3}>
                  {gifts.map((gift) => {
                    const remaining = parseFloat(gift.estimated_cost) - parseFloat(gift.contributed_amount);
                    const percent = Math.min(100, Math.round((parseFloat(gift.contributed_amount) / parseFloat(gift.estimated_cost)) * 100));

                    return (
                      <Grid item xs={12} sm={6} key={gift.id}>
                        <Card className="hover-scale" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                          {gift.image_url && (
                            <Box
                              component="img"
                              src={`${api.defaults.baseURL}${gift.image_url}`}
                              alt={gift.name}
                              sx={{ height: 180, width: '100%', objectFit: 'cover' }}
                            />
                          )}
                          <CardContent sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', justify: 'space-between' }}>
                            <Box>
                              <Typography variant="h6" color="secondary.main" sx={{ mb: 1 }}>{gift.name}</Typography>
                              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                                {gift.description || 'Registry gift item.'}
                              </Typography>
                            </Box>

                            <Box sx={{ mt: 'auto' }}>
                              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                                <Typography variant="caption" color="text.secondary">Remaining: ₹{remaining.toLocaleString()}</Typography>
                                <Typography variant="caption" sx={{ fontWeight: 600 }}>{percent}% funded</Typography>
                              </Box>

                              <LinearProgress variant="determinate" value={percent} sx={{ height: 6, borderRadius: 3, mb: 2 }} />

                              {(() => {
                                const isFullyFunded = remaining <= 0 || gift.status === 'FUNDED';
                                return (
                                  <Button
                                    fullWidth
                                    variant="contained"
                                    color="primary"
                                    startIcon={<CardGiftcard />}
                                    disabled={isFullyFunded}
                                    onClick={!isFullyFunded ? () => handleOpenContrib(gift) : undefined}
                                    sx={isFullyFunded ? { color: 'success.contrastText', bgcolor: 'success.light' } : undefined}
                                  >
                                    {isFullyFunded ? 'Gift Fully Sponsored!' : 'Contribute Cash'}
                                  </Button>
                                );
                              })()}
                            </Box>
                          </CardContent>
                        </Card>
                      </Grid>
                    );
                  })}
                </Grid>
              )}
            </Paper>
          </Grid>
        </Grid>
      </Container>

      {/* CONTRIBUTION MODAL DIALOG */}
      <Dialog open={openContribModal} onClose={() => !paymentLoading && setOpenContribModal(false)} maxWidth="xs" fullWidth>
        {selectedGift && (
          <form onSubmit={handleContributeSubmit}>
            <DialogTitle>Contribute to {selectedGift.name}</DialogTitle>
            <DialogContent>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Target Cost: ₹{parseFloat(selectedGift.estimated_cost).toLocaleString()} <br />
                Remaining Balance: <strong>₹{(parseFloat(selectedGift.estimated_cost) - parseFloat(selectedGift.contributed_amount)).toLocaleString()}</strong>
              </Typography>

              {/* Quick suggestions */}
              <Stack direction="row" spacing={1} sx={{ mb: 3 }}>
                {[500, 1000, 2000, 5000].map((amt) => {
                  const remaining = parseFloat(selectedGift.estimated_cost) - parseFloat(selectedGift.contributed_amount);
                  if (amt > remaining) return null;
                  return (
                    <Button
                      key={amt}
                      variant="outlined"
                      size="small"
                      onClick={() => setContribAmount(amt.toString())}
                    >
                      ₹{amt}
                    </Button>
                  );
                })}
              </Stack>

              <TextField
                fullWidth
                label="Contribution Amount"
                type="number"
                required
                disabled={paymentLoading}
                value={contribAmount}
                onChange={(e) => setContribAmount(e.target.value)}
                InputProps={{
                  startAdornment: <InputAdornment position="start">₹</InputAdornment>,
                }}
                sx={{ mb: 2 }}
              />

              <FormControlLabel
                control={
                  <Checkbox
                    checked={anonymous}
                    onChange={(e) => setAnonymous(e.target.checked)}
                    disabled={paymentLoading}
                  />
                }
                label="Contribute anonymously (hide name from public feed)"
              />
              <Typography variant="caption" display="block" color="text.secondary" sx={{ mt: 0.5 }}>
                Note: The wedding hosts will still be able to see your identity on reports.
              </Typography>
            </DialogContent>
            <DialogActions>
              <Button onClick={() => setOpenContribModal(false)} disabled={paymentLoading}>
                Cancel
              </Button>
              <Button
                type="submit"
                variant="contained"
                color="primary"
                disabled={paymentLoading}
              >
                {paymentLoading ? <CircularProgress size={24} /> : 'Pay Contribution'}
              </Button>
            </DialogActions>
          </form>
        )}
      </Dialog>
    </Box>
  );
};

export default EventDetails;
