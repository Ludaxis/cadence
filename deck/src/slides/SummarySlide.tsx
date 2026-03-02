import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';

const metrics = [
  { label: 'Signal Tiers', value: '5' },
  { label: 'Core Signals', value: '6' },
  { label: 'Flow States', value: '4' },
  { label: 'Adjustment Rules', value: '4' },
  { label: 'Analytics Events', value: '40' },
  { label: 'Unit Tests', value: '52' },
  { label: 'Tick Latency', value: '<0.5ms' },
  { label: 'GC Pressure', value: '0' },
];

export default function SummarySlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    const tl = gsap.timeline({
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
    });
    tl.from('.summary-metric', {
      opacity: 0,
      scale: 0.8,
      stagger: { each: 0.06, from: 'edges' },
      duration: 0.4,
      ease: 'back.out(1.5)',
    }).from('.summary-cta', {
      opacity: 0,
      y: 20,
      duration: 0.6,
    }, '-=0.2');
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="summary">
      <Typography
        variant="h2"
        sx={{
          background: 'linear-gradient(135deg, #00d4ff 0%, #b388ff 100%)',
          WebkitBackgroundClip: 'text',
          WebkitTextFillColor: 'transparent',
          mb: 1,
          fontWeight: 800,
        }}
      >
        Cadence DDA SDK
      </Typography>
      <Typography
        variant="h5"
        sx={{ color: 'text.secondary', mb: 5, fontWeight: 400 }}
      >
        Every number at a glance
      </Typography>

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: 'repeat(2, 1fr)', sm: 'repeat(4, 1fr)' },
          gap: 2,
          maxWidth: 700,
          width: '100%',
          mb: 5,
        }}
      >
        {metrics.map((m) => (
          <Paper
            key={m.label}
            className="summary-metric"
            elevation={0}
            sx={{
              p: 2,
              textAlign: 'center',
              bgcolor: 'rgba(0,212,255,0.04)',
              border: '1px solid rgba(0,212,255,0.1)',
            }}
          >
            <Typography
              sx={{ color: 'primary.main', fontWeight: 800, fontSize: '1.4rem', mb: 0.3 }}
            >
              {m.value}
            </Typography>
            <Typography
              variant="body2"
              sx={{ color: 'text.secondary', fontSize: '0.75rem' }}
            >
              {m.label}
            </Typography>
          </Paper>
        ))}
      </Box>

      <Box className="summary-cta" sx={{ textAlign: 'center' }}>
        <Button
          variant="outlined"
          size="large"
          sx={{
            px: 5,
            py: 1.5,
            borderColor: 'primary.main',
            color: 'primary.main',
            fontWeight: 700,
            fontSize: '1rem',
            borderRadius: 3,
            animation: 'ctaPulse 2s ease-in-out infinite',
            '@keyframes ctaPulse': {
              '0%, 100%': { boxShadow: '0 0 0 0 rgba(0,212,255,0.3)' },
              '50%': { boxShadow: '0 0 20px 4px rgba(0,212,255,0.15)' },
            },
            '&:hover': {
              bgcolor: 'rgba(0,212,255,0.08)',
              borderColor: 'primary.main',
            },
          }}
        >
          Explore on GitHub →
        </Button>
        <Typography
          variant="body2"
          sx={{ color: 'rgba(255,255,255,0.3)', mt: 2, fontSize: '0.8rem' }}
        >
          by Ludaxis
        </Typography>
      </Box>
    </SlideContainer>
  );
}
