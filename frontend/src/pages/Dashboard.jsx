import React, { useState, useEffect } from 'react';
import {
  Container, Typography, Box, Grid, Card, CardContent, Button, Tabs, Tab,
  Dialog, DialogTitle, DialogContent, DialogActions, TextField, Divider,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
  IconButton, Alert, CircularProgress, LinearProgress, Stack
} from '@mui/material';
import {
  Add, Edit, Delete, UploadFile, FileDownload, ContentCopy,
  Favorite, Loyalty, Group, AccountBalanceWallet, Link as LinkIcon, Logout
} from '@mui/icons-material';
import { useDispatch, useSelector } from 'react-redux';
import { logout } from '../store/authSlice';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';

const Dashboard = () => {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { user } = useSelector((state) => state.auth);
  const baseUrl = api.defaults.baseURL; //"http://localhost:5000"

  // States
  const [activeTab, setActiveTab] = useState(0);
  const [event, setEvent] = useState(null);
  const [gifts, setGifts] = useState([]);
  const [invitees, setInvitees] = useState([]);
  const [contributions, setContributions] = useState([]);
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Modals / Dialogs
  const [openEventModal, setOpenEventModal] = useState(false);
  const [openGiftModal, setOpenGiftModal] = useState(false);
  const [openInviteeModal, setOpenInviteeModal] = useState(false);

  // Forms states
  const [eventForm, setEventForm] = useState({
    event_name: '', groom_name: '', bride_name: '', event_date: '', venue: '', description: '', cover_photo_url: ''
  });
  const [giftForm, setGiftForm] = useState({
    name: '', description: '', estimated_cost: '', image_url: ''
  });
  const [giftPreview, setGiftPreview] = useState(null);
  const [inviteeForm, setInviteeForm] = useState({
    name: '', phone: '', email: '', relation: ''
  });

  const [selectedGiftId, setSelectedGiftId] = useState(null);
  const [csvFile, setCsvFile] = useState(null);
  const [uploadSuccess, setUploadSuccess] = useState(null);

  // Fetch Host data
  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      // Get events
      const eventRes = await api.get('/api/events');
      if (eventRes.data.length > 0) {
        const activeEvent = eventRes.data[0];
        setEvent(activeEvent);

        // Parallel fetching
        const [giftsRes, inviteesRes, summaryRes, contribsRes] = await Promise.all([
          api.get(`/api/events/${activeEvent.id}/gifts`),
          api.get(`/api/events/${activeEvent.id}/invitees`),
          api.get(`/api/events/${activeEvent.id}/reports/summary`),
          api.get(`/api/events/${activeEvent.id}/contributions`)
        ]);

        setGifts(giftsRes.data);
        setInvitees(inviteesRes.data);
        setSummary(summaryRes.data);
        setContributions(contribsRes.data);
      } else {
        // No event created yet
        setEvent(null);
      }
    } catch (err) {
      setError('Failed to fetch dashboard data. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleLogout = () => {
    dispatch(logout());
    navigate('/login');
  };

  // Event handler
  const handleEventSubmit = async (e) => {
    e.preventDefault();
    try {
      if (event) {
        // Edit existing
        const res = await api.put(`/api/events/${event.id}`, eventForm);
        setEvent(res.data);
      } else {
        // Create new
        const res = await api.post('/api/events', eventForm);
        setEvent(res.data);
      }
      setOpenEventModal(false);
      fetchData();
    } catch (err) {
      setError('Failed to save wedding event.');
    }
  };

  // Gift handlers
  const handleGiftSubmit = async (e) => {
    e.preventDefault();
    try {
      if (selectedGiftId) {
        await api.put(`/api/gifts/${selectedGiftId}`, giftForm);
      } else {
        await api.post(`/api/events/${event.id}/gifts`, giftForm);
      }
      setOpenGiftModal(false);
      setGiftForm({ name: '', description: '', estimated_cost: '', image_url: '' });
      setSelectedGiftId(null);
      fetchData();
    } catch (err) {
      setError('Failed to save gift item.');
    }
  };

  const handleGiftImageUpload = async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    // Immediate client-side preview
    try {
      const reader = new FileReader();
      reader.onload = (ev) => {
        setGiftPreview(ev.target.result);
      };
      reader.readAsDataURL(file);
    } catch (ex) {
      console.warn('Preview generation failed', ex);
      setGiftPreview(null);
    }

    const formData = new FormData();
    formData.append('file', file);
    try {
      // Let the browser set Content-Type with proper boundary
      const res = await api.post('/api/gifts/upload-image', formData);
      setGiftForm({ ...giftForm, image_url: res.data.image_url });
    } catch (err) {
      console.error('Image upload error', err);
      const detail = err.response?.data?.detail || 'Failed to upload image.';
      setError(detail);
    }
  };

  const handleEditGift = (gift) => {
    setSelectedGiftId(gift.id);
    setGiftForm({
      name: gift.name,
      description: gift.description || '',
      estimated_cost: gift.estimated_cost,
      image_url: gift.image_url || ''
    });
    setOpenGiftModal(true);
  };

  const handleDeleteGift = async (giftId) => {
    if (window.confirm('Are you sure you want to delete this gift item?')) {
      try {
        await api.delete(`/api/gifts/${giftId}`);
        fetchData();
      } catch (err) {
        setError('Failed to delete gift.');
      }
    }
  };

  // Invitee handlers
  const handleInviteeSubmit = async (e) => {
    e.preventDefault();
    try {
      await api.post(`/api/events/${event.id}/invitees`, inviteeForm);
      setOpenInviteeModal(false);
      setInviteeForm({ name: '', phone: '', email: '', relation: '' });
      fetchData();
    } catch (err) {
      setError('Failed to add invitee.');
    }
  };

  const handleCsvUpload = async (e) => {
    e.preventDefault();
    if (!csvFile) return;

    const formData = new FormData();
    formData.append('file', csvFile);
    try {
      await api.post(`/api/events/${event.id}/invitees/upload`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      setUploadSuccess('CSV Uploaded and Invitees added successfully!');
      setCsvFile(null);
      fetchData();
    } catch (err) {
      setError('Failed to upload CSV. Ensure the format has "name" and "phone" columns.');
    }
  };

  const handleDeleteInvitee = async (inviteeId) => {
    if (window.confirm('Remove this invitee?')) {
      try {
        await api.delete(`/api/events/${event.id}/invitees/${inviteeId}`);
        fetchData();
      } catch (err) {
        setError('Failed to remove invitee.');
      }
    }
  };

  // Reports Export
  const handleExportCSV = async () => {
    try {
      const response = await api.get(`/api/events/${event.id}/reports/export`, {
        responseType: 'blob'
      });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `contributions_wedding_${event.id}.csv`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      setError('Failed to export CSV report.');
    }
  };

  const copyToClipboard = (text) => {
    navigator.clipboard.writeText(text);
    alert('Link copied to clipboard!');
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <CircularProgress color="primary" />
      </Box>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      {/* Top Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
        <Typography variant="h3" className="gold-gradient-text" sx={{ fontWeight: 800 }}>
          Shagun Host Dashboard
        </Typography>
        <Stack direction="row" spacing={2}>
          <Button variant="outlined" color="secondary" startIcon={<Logout />} onClick={handleLogout}>
            Logout
          </Button>
        </Stack>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>{error}</Alert>}
      {uploadSuccess && <Alert severity="success" sx={{ mb: 3 }} onClose={() => setUploadSuccess(null)}>{uploadSuccess}</Alert>}

      {/* 1. NO EVENT STATE */}
      {!event ? (
        <Card sx={{ textAlign: 'center', py: 8, px: 4 }}>
          <Favorite sx={{ fontSize: 80, color: 'primary.main', mb: 3 }} />
          <Typography variant="h4" gutterBottom>
            Welcome, {user?.name}!
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 4, maxWidth: 600, mx: 'auto' }}>
            To start receiving gift contributions, create your wedding event registry. Fill in details like Bride name, Groom name, event date, venue, and description.
          </Typography>
          <Button
            variant="contained"
            size="large"
            onClick={() => {
              setEventForm({
                event_name: 'Our Wedding Ceremony', groom_name: '', bride_name: '', event_date: '', venue: '', description: '', cover_photo_url: ''
              });
              setOpenEventModal(true);
            }}
          >
            Create Wedding Event
          </Button>
        </Card>
      ) : (
        <>
          {/* Header Info */}
          <Paper sx={{ p: 3, mb: 4, background: 'linear-gradient(to right, #800020, #4a0012)', color: 'white', position: 'relative', overflow: 'hidden' }}>
            <Box sx={{ position: 'relative', zIndex: 1 }}>
              <Typography variant="h4" sx={{ fontFamily: 'Playfair Display', mb: 1 }}>
                {event.event_name}
              </Typography>
              <Typography variant="h6" sx={{ opacity: 0.9, mb: 2 }}>
                {event.groom_name} ❤️ {event.bride_name} — {new Date(event.event_date).toLocaleDateString()}
              </Typography>
              <Typography variant="body2" sx={{ opacity: 0.8, mb: 3 }}>
                Venue: {event.venue}
              </Typography>
              <Button
                variant="outlined"
                sx={{ color: 'white', borderColor: 'white', '&:hover': { bg: 'rgba(255,255,255,0.1)' } }}
                startIcon={<Edit />}
                onClick={() => {
                  setEventForm({
                    event_name: event.event_name,
                    groom_name: event.groom_name,
                    bride_name: event.bride_name,
                    event_date: event.event_date,
                    venue: event.venue,
                    description: event.description || '',
                    cover_photo_url: event.cover_photo_url || ''
                  });
                  setOpenEventModal(true);
                }}
              >
                Edit Wedding Details
              </Button>
            </Box>
          </Paper>

          {/* Metrics summary cards */}
          {summary && (
            <Grid container spacing={3} sx={{ mb: 4 }}>
              <Grid item xs={12} sm={6} md={3}>
                <Card>
                  <CardContent>
                    <Stack direction="row" spacing={2} alignItems="center">
                      <Loyalty color="primary" sx={{ fontSize: 40 }} />
                      <Box>
                        <Typography color="text.secondary" variant="body2">Gifts Count</Typography>
                        <Typography variant="h5" sx={{ fontWeight: 700 }}>
                          {summary.funded_gifts} / {summary.total_gifts} Funded
                        </Typography>
                      </Box>
                    </Stack>
                  </CardContent>
                </Card>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Card>
                  <CardContent>
                    <Stack direction="row" spacing={2} alignItems="center">
                      <Group color="secondary" sx={{ fontSize: 40 }} />
                      <Box>
                        <Typography color="text.secondary" variant="body2">Invited Guests</Typography>
                        <Typography variant="h5" sx={{ fontWeight: 700 }}>{summary.total_invitees}</Typography>
                      </Box>
                    </Stack>
                  </CardContent>
                </Card>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Card>
                  <CardContent>
                    <Stack direction="row" spacing={2} alignItems="center">
                      <AccountBalanceWallet color="success" sx={{ fontSize: 40 }} />
                      <Box>
                        <Typography color="text.secondary" variant="body2">Total Received</Typography>
                        <Typography variant="h5" sx={{ fontWeight: 700 }}>₹{summary.received_amount.toLocaleString()}</Typography>
                      </Box>
                    </Stack>
                  </CardContent>
                </Card>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Card>
                  <CardContent>
                    <Box sx={{ width: '100%' }}>
                      <Typography color="text.secondary" variant="body2" gutterBottom>
                        Total Progress ({summary.funding_percentage}%)
                      </Typography>
                      <LinearProgress variant="determinate" value={summary.funding_percentage} color="primary" sx={{ height: 10, borderRadius: 5, mb: 1 }} />
                      <Typography variant="caption" color="text.secondary">
                        Target: ₹{summary.target_amount.toLocaleString()}
                      </Typography>
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          )}

          {/* Tab Navigation */}
          <Tabs value={activeTab} onChange={(e, val) => setActiveTab(val)} color="primary" sx={{ borderBottom: 1, borderColor: 'divider', mb: 4 }}>
            <Tab label="Gift Items Catalog" />
            <Tab label="Invitee List" />
            <Tab label="Contributions & Reports" />
          </Tabs>

          {/* TAB 0: GIFT CATALOG */}
          {activeTab === 0 && (
            <Box>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
                <Typography variant="h5" color="secondary.main" sx={{ fontWeight: 600 }}>
                  Registry Gifts Catalog
                </Typography>
                <Button
                  variant="contained"
                  startIcon={<Add />}
                  onClick={() => {
                    setSelectedGiftId(null);
                    setGiftForm({ name: '', description: '', estimated_cost: '', image_url: '' });
                    setOpenGiftModal(true);
                  }}
                >
                  Add New Gift
                </Button>
              </Box>

              {gifts.length === 0 ? (
                <Paper sx={{ p: 4, textAlign: 'center' }}>
                  <Typography variant="body1" color="text.secondary">
                    No gifts added yet. Click "Add New Gift" to add items like honeymoon registry, kitchen appliances, furniture etc.
                  </Typography>
                </Paper>
              ) : (
                <Grid container spacing={3}>
                  {gifts.map((gift) => (
                    <Grid item xs={12} sm={6} md={4} key={gift.id}>
                      <Card className="hover-scale" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                        {gift.image_url && (
                          <Box
                            component="img"
                            src={`${baseUrl}${gift.image_url}`}
                            alt={gift.name}
                            sx={{ height: 200, width: '100%', objectFit: 'cover' }}
                          />
                        )}
                        <CardContent sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', justify: 'space-between' }}>
                          <Box>
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                              <Typography variant="h6" color="secondary.main">{gift.name}</Typography>
                              <Paper sx={{ px: 1.5, py: 0.5, bgcolor: gift.status === 'FUNDED' ? 'success.light' : 'primary.light', color: 'white', borderRadius: 4, fontSize: '0.75rem', fontWeight: 700 }}>
                                {gift.status}
                              </Paper>
                            </Box>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                              {gift.description || 'No description provided.'}
                            </Typography>
                          </Box>

                          <Box sx={{ mt: 'auto' }}>
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                              <Typography variant="body2" sx={{ fontWeight: 600 }}>₹{parseFloat(gift.contributed_amount).toLocaleString()}</Typography>
                              <Typography variant="body2" color="text.secondary">Target: ₹{parseFloat(gift.estimated_cost).toLocaleString()}</Typography>
                            </Box>
                            <LinearProgress
                              variant="determinate"
                              value={Math.min(100, (parseFloat(gift.contributed_amount) / parseFloat(gift.estimated_cost)) * 100)}
                              sx={{ height: 8, borderRadius: 4, mb: 2 }}
                            />
                            <Divider sx={{ mb: 2 }} />
                            <Stack direction="row" spacing={1} justifyContent="flex-end">
                              <IconButton color="primary" onClick={() => handleEditGift(gift)}>
                                <Edit />
                              </IconButton>
                              <IconButton color="error" onClick={() => handleDeleteGift(gift.id)}>
                                <Delete />
                              </IconButton>
                            </Stack>
                          </Box>
                        </CardContent>
                      </Card>
                    </Grid>
                  ))}
                </Grid>
              )}
            </Box>
          )}

          {/* TAB 1: INVITEE MANAGEMENT */}
          {activeTab === 1 && (
            <Box>
              <Grid container spacing={4}>
                <Grid item xs={12} md={8}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
                    <Typography variant="h5" color="secondary.main" sx={{ fontWeight: 600 }}>
                      Invited Guests List
                    </Typography>
                    <Button
                      variant="contained"
                      startIcon={<Add />}
                      onClick={() => setOpenInviteeModal(true)}
                    >
                      Add Invitee
                    </Button>
                  </Box>

                  {invitees.length === 0 ? (
                    <Paper sx={{ p: 4, textAlign: 'center' }}>
                      <Typography variant="body1" color="text.secondary">
                        No guests invited yet. Add invitees manually or upload a CSV.
                      </Typography>
                    </Paper>
                  ) : (
                    <TableContainer component={Paper}>
                      <Table>
                        <TableHead>
                          <TableRow>
                            <TableCell>Name</TableCell>
                            <TableCell>Phone</TableCell>
                            <TableCell>Relation</TableCell>
                            <TableCell>Invite Link</TableCell>
                            <TableCell>Status</TableCell>
                            <TableCell align="right">Actions</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {invitees.map((guest) => {
                            const inviteLink = `${baseUrl}/invite/${guest.invite_token}`;
                            return (
                              <TableRow key={guest.id}>
                                <TableCell sx={{ fontWeight: 600 }}>{guest.name}</TableCell>
                                <TableCell>{guest.phone}</TableCell>
                                <TableCell>{guest.relation || 'N/A'}</TableCell>
                                <TableCell>
                                  <IconButton size="small" color="primary" onClick={() => copyToClipboard(inviteLink)}>
                                    <ContentCopy fontSize="small" />
                                  </IconButton>
                                  <Typography variant="caption" sx={{ ml: 1, color: 'text.secondary', wordBreak: 'break-all' }}>
                                    /invite/{guest.invite_token.substring(0, 8)}...
                                  </Typography>
                                </TableCell>
                                <TableCell>
                                  <Paper sx={{ display: 'inline-block', px: 1, py: 0.25, fontSize: '0.75rem', fontWeight: 600, bgcolor: guest.status === 'opened' ? 'warning.light' : 'success.light', color: 'white', borderRadius: 1 }}>
                                    {guest.status}
                                  </Paper>
                                </TableCell>
                                <TableCell align="right">
                                  <IconButton color="error" onClick={() => handleDeleteInvitee(guest.id)}>
                                    <Delete />
                                  </IconButton>
                                </TableCell>
                              </TableRow>
                            );
                          })}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  )}
                </Grid>

                {/* CSV Import */}
                <Grid item xs={12} md={4}>
                  <Card>
                    <CardContent>
                      <Typography variant="h6" gutterBottom color="secondary.main" sx={{ mb: 2 }}>
                        Bulk CSV Upload
                      </Typography>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                        Upload a CSV list of your invitees. The CSV must have column headers <strong>name</strong> and <strong>phone</strong>. Optional columns include <strong>email</strong> and <strong>relation</strong>.
                      </Typography>

                      <form onSubmit={handleCsvUpload}>
                        <Box sx={{ border: '2px dashed #b8860b', p: 3, textAlign: 'center', borderRadius: 2, mb: 3, cursor: 'pointer', position: 'relative' }}>
                          <input
                            type="file"
                            accept=".csv"
                            style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', opacity: 0, cursor: 'pointer' }}
                            onChange={(e) => setCsvFile(e.target.files[0])}
                          />
                          <UploadFile color="primary" sx={{ fontSize: 40, mb: 1 }} />
                          <Typography variant="body2" sx={{ fontWeight: 600 }}>
                            {csvFile ? csvFile.name : 'Click or Drag CSV File Here'}
                          </Typography>
                        </Box>
                        <Button
                          type="submit"
                          fullWidth
                          variant="contained"
                          color="primary"
                          disabled={!csvFile}
                        >
                          Upload Guest List
                        </Button>
                      </form>
                    </CardContent>
                  </Card>
                </Grid>
              </Grid>
            </Box>
          )}

          {/* TAB 2: REPORTS & TRANSACTIONS */}
          {activeTab === 2 && (
            <Box>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
                <Typography variant="h5" color="secondary.main" sx={{ fontWeight: 600 }}>
                  Contributions List
                </Typography>
                <Button
                  variant="outlined"
                  startIcon={<FileDownload />}
                  onClick={handleExportCSV}
                >
                  Export CSV Report
                </Button>
              </Box>

              {contributions.length === 0 ? (
                <Paper sx={{ p: 4, textAlign: 'center' }}>
                  <Typography variant="body1" color="text.secondary">
                    No contributions received yet. Gift links will appear here once guest payments succeed.
                  </Typography>
                </Paper>
              ) : (
                <TableContainer component={Paper}>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Guest</TableCell>
                        <TableCell>Gift Item</TableCell>
                        <TableCell>Amount</TableCell>
                        <TableCell>Anonymous</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell>Date</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {contributions.map((c) => (
                        <TableRow key={c.id}>
                          <TableCell sx={{ fontWeight: 600 }}>{c.invitee_name}</TableCell>
                          <TableCell>{c.gift_item?.name || 'Gift Item'}</TableCell>
                          <TableCell sx={{ color: 'success.main', fontWeight: 600 }}>₹{parseFloat(c.amount).toLocaleString()}</TableCell>
                          <TableCell>{c.anonymous ? 'Yes' : 'No'}</TableCell>
                          <TableCell>
                            <Paper sx={{ display: 'inline-block', px: 1, py: 0.25, fontSize: '0.75rem', fontWeight: 600, bgcolor: c.status === 'SUCCESS' ? 'success.main' : 'warning.main', color: 'white', borderRadius: 1 }}>
                              {c.status}
                            </Paper>
                          </TableCell>
                          <TableCell>{new Date(c.created_at).toLocaleDateString()}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </Box>
          )}
        </>
      )}

      {/* EVENT EDIT/CREATE DIALOG */}
      <Dialog open={openEventModal} onClose={() => setOpenEventModal(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{event ? 'Edit Wedding Event Details' : 'Create Wedding Event Registry'}</DialogTitle>
        <form onSubmit={handleEventSubmit}>
          <DialogContent>
            <TextField
              fullWidth label="Wedding Name (e.g. Rahul & Priya Wedding)" required margin="normal"
              value={eventForm.event_name} onChange={(e) => setEventForm({ ...eventForm, event_name: e.target.value })}
            />
            <Stack direction="row" spacing={2} sx={{ mt: 1 }}>
              <TextField
                fullWidth label="Groom Name" required margin="normal"
                value={eventForm.groom_name} onChange={(e) => setEventForm({ ...eventForm, groom_name: e.target.value })}
              />
              <TextField
                fullWidth label="Bride Name" required margin="normal"
                value={eventForm.bride_name} onChange={(e) => setEventForm({ ...eventForm, bride_name: e.target.value })}
              />
            </Stack>
            <TextField
              fullWidth label="Wedding Date" type="date" required margin="normal" InputLabelProps={{ shrink: true }}
              value={eventForm.event_date} onChange={(e) => setEventForm({ ...eventForm, event_date: e.target.value })}
            />
            <TextField
              fullWidth label="Venue" required margin="normal"
              value={eventForm.venue} onChange={(e) => setEventForm({ ...eventForm, venue: e.target.value })}
            />
            <TextField
              fullWidth label="Description" multiline rows={3} margin="normal"
              value={eventForm.description} onChange={(e) => setEventForm({ ...eventForm, description: e.target.value })}
            />
            <TextField
              fullWidth label="Cover Photo URL (Optional)" margin="normal"
              value={eventForm.cover_photo_url} onChange={(e) => setEventForm({ ...eventForm, cover_photo_url: e.target.value })}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpenEventModal(false)}>Cancel</Button>
            <Button type="submit" variant="contained" color="primary">Save Event</Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* GIFT ADD/EDIT DIALOG */}
      <Dialog open={openGiftModal} onClose={() => setOpenGiftModal(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{selectedGiftId ? 'Edit Gift Item' : 'Add Gift to Registry'}</DialogTitle>
        <form onSubmit={handleGiftSubmit}>
          <DialogContent>
            <TextField
              fullWidth label="Gift Name" required margin="normal"
              value={giftForm.name} onChange={(e) => setGiftForm({ ...giftForm, name: e.target.value })}
            />
            <TextField
              fullWidth label="Description" multiline rows={2} margin="normal"
              value={giftForm.description} onChange={(e) => setGiftForm({ ...giftForm, description: e.target.value })}
            />
            <TextField
              fullWidth label="Estimated Cost (₹)" type="number" required margin="normal"
              value={giftForm.estimated_cost} onChange={(e) => setGiftForm({ ...giftForm, estimated_cost: e.target.value })}
            />

            <Box sx={{ mt: 2, mb: 1 }}>
              <Typography variant="body2" color="text.secondary" gutterBottom>Upload Gift Image</Typography>
              <Stack direction="row" spacing={2} alignItems="center">
                <Button variant="outlined" component="label" size="small" startIcon={<UploadFile />}>
                  Choose Image
                  <input type="file" hidden accept="image/*" onChange={handleGiftImageUpload} />
                </Button>
                {giftForm.image_url && <Typography variant="caption" color="success.main">Uploaded!</Typography>}
              </Stack>
              {(giftPreview || giftForm.image_url) && (
                <Box sx={{ mt: 2 }}>
                  <Typography variant="caption" color="text.secondary">Preview</Typography>
                  <Box
                    component="img"
                    src={giftPreview ? giftPreview : `${api.defaults.baseURL}${giftForm.image_url}`}
                    alt="Gift preview"
                    sx={{ width: 140, height: 100, objectFit: 'cover', borderRadius: 1, border: '1px solid rgba(0,0,0,0.06)', mt: 1 }}
                  />
                </Box>
              )}
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpenGiftModal(false)}>Cancel</Button>
            <Button type="submit" variant="contained" color="primary">Save Gift</Button>
          </DialogActions>
        </form>
      </Dialog>

      {/* INVITEE ADD DIALOG */}
      <Dialog open={openInviteeModal} onClose={() => setOpenInviteeModal(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Add Guest Invitee</DialogTitle>
        <form onSubmit={handleInviteeSubmit}>
          <DialogContent>
            <TextField
              fullWidth label="Guest Full Name" required margin="normal"
              value={inviteeForm.name} onChange={(e) => setInviteeForm({ ...inviteeForm, name: e.target.value })}
            />
            <TextField
              fullWidth label="Phone Number" placeholder="e.g. +919876543210" required margin="normal"
              value={inviteeForm.phone} onChange={(e) => setInviteeForm({ ...inviteeForm, phone: e.target.value })}
            />
            <TextField
              fullWidth label="Email (Optional)" type="email" margin="normal"
              value={inviteeForm.email} onChange={(e) => setInviteeForm({ ...inviteeForm, email: e.target.value })}
            />
            <TextField
              fullWidth label="Relation (e.g. Family, Friend)" margin="normal"
              value={inviteeForm.relation} onChange={(e) => setInviteeForm({ ...inviteeForm, relation: e.target.value })}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpenInviteeModal(false)}>Cancel</Button>
            <Button type="submit" variant="contained" color="primary">Add Invitee</Button>
          </DialogActions>
        </form>
      </Dialog>
    </Container>
  );
};

export default Dashboard;
