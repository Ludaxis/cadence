import { createTheme } from '@mui/material/styles';

const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#00d4ff' },
    secondary: { main: '#b388ff' },
    background: {
      default: '#0a0e27',
      paper: '#111638',
    },
    text: {
      primary: '#e8eaf6',
      secondary: '#9fa8da',
    },
  },
  typography: {
    fontFamily: "'Inter', sans-serif",
    h1: { fontWeight: 800, fontSize: '3.5rem', letterSpacing: '-0.02em' },
    h2: { fontWeight: 700, fontSize: '2.5rem', letterSpacing: '-0.01em' },
    h3: { fontWeight: 600, fontSize: '1.75rem' },
    h4: { fontWeight: 600, fontSize: '1.35rem' },
    h5: { fontWeight: 500, fontSize: '1.1rem' },
    body1: { fontSize: '1.05rem', lineHeight: 1.7 },
    body2: { fontSize: '0.95rem', lineHeight: 1.6 },
  },
  shape: { borderRadius: 12 },
  components: {
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 600,
          letterSpacing: '0.05em',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
        },
      },
    },
  },
});

export default theme;
