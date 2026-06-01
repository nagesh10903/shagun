import { createTheme } from '@mui/material/styles';

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#b8860b', // Dark Goldenrod
      light: '#d4af37', // Gold
      dark: '#8b6508',
      contrastText: '#ffffff',
    },
    secondary: {
      main: '#800020', // Burgundy
      light: '#a52a2a', // Brown/Red
      dark: '#4a0012',
      contrastText: '#ffffff',
    },
    background: {
      default: '#fcfbf7', // Warm Cream
      paper: '#ffffff',
    },
    text: {
      primary: '#2c2520', // Deep charcoal with warmth
      secondary: '#6b5e53',
    },
  },
  typography: {
    fontFamily: '"Outfit", "Playfair Display", "Roboto", "Helvetica", "Arial", sans-serif',
    h1: {
      fontFamily: '"Playfair Display", serif',
      fontWeight: 700,
    },
    h2: {
      fontFamily: '"Playfair Display", serif',
      fontWeight: 700,
    },
    h3: {
      fontFamily: '"Playfair Display", serif',
      fontWeight: 600,
    },
    h4: {
      fontFamily: '"Playfair Display", serif',
      fontWeight: 600,
    },
    h5: {
      fontFamily: '"Outfit", sans-serif',
      fontWeight: 600,
    },
    h6: {
      fontFamily: '"Outfit", sans-serif',
      fontWeight: 500,
    },
    button: {
      textTransform: 'none',
      fontWeight: 600,
    },
  },
  shape: {
    borderRadius: 12,
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 24,
          padding: '8px 24px',
          boxShadow: 'none',
          transition: 'all 0.3s ease',
          '&:hover': {
            boxShadow: '0 4px 12px rgba(184, 134, 11, 0.2)',
            transform: 'translateY(-1px)',
          },
        },
        containedPrimary: {
          background: 'linear-gradient(45deg, #b8860b 30%, #d4af37 90%)',
          color: '#ffffff',
        },
        containedSecondary: {
          background: 'linear-gradient(45deg, #800020 30%, #a52a2a 90%)',
          color: '#ffffff',
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          boxShadow: '0 4px 20px rgba(107, 94, 83, 0.08)',
          border: '1px solid rgba(212, 175, 55, 0.15)',
          overflow: 'hidden',
        },
      },
    },
  },
});

export default theme;
