import { useRef } from 'react';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';

export default function TitleSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    const tl = gsap.timeline({ defaults: { ease: 'power3.out' } });
    tl.from('.title-logo', { opacity: 0, scale: 0.8, duration: 1 })
      .from('.title-main', { opacity: 0, y: 30, duration: 0.8 }, '-=0.4')
      .from('.title-sub', { opacity: 0, y: 20, duration: 0.6 }, '-=0.3')
      .from('.title-badge', { opacity: 0, scale: 0.9, duration: 0.5 }, '-=0.2')
      .from('.title-hint', { opacity: 0, duration: 0.8 }, '-=0.1');
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="title">
      {/* Background glow */}
      <Box
        sx={{
          position: 'absolute',
          width: 500,
          height: 500,
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(0,212,255,0.08) 0%, transparent 70%)',
          top: '50%',
          left: '50%',
          transform: 'translate(-50%, -50%)',
          pointerEvents: 'none',
        }}
      />

      <Box sx={{ textAlign: 'center', position: 'relative', zIndex: 1 }}>
        {/* Logo mark */}
        <Typography
          className="title-logo"
          sx={{
            fontSize: '4rem',
            mb: 2,
            filter: 'drop-shadow(0 0 30px rgba(0,212,255,0.4))',
          }}
        >
          🎵
        </Typography>

        <Typography
          className="title-main"
          variant="h1"
          sx={{
            fontSize: { xs: '2.5rem', md: '4.5rem' },
            background: 'linear-gradient(135deg, #00d4ff 0%, #b388ff 100%)',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            mb: 2,
          }}
        >
          Cadence
        </Typography>

        <Typography
          className="title-sub"
          variant="h3"
          sx={{ color: 'text.secondary', fontWeight: 400, mb: 4, fontSize: { xs: '1.1rem', md: '1.5rem' } }}
        >
          Dynamic Difficulty Adjustment SDK
        </Typography>

        <Box
          className="title-badge"
          sx={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 1,
            px: 3,
            py: 1,
            borderRadius: 6,
            border: '1px solid rgba(179,136,255,0.3)',
            bgcolor: 'rgba(179,136,255,0.08)',
            mb: 6,
          }}
        >
          <Typography variant="body2" sx={{ color: '#b388ff', fontWeight: 500 }}>
            by Ludaxis
          </Typography>
        </Box>

        <Typography
          className="title-hint"
          variant="body2"
          sx={{
            color: 'rgba(255,255,255,0.3)',
            display: 'block',
            mt: 4,
            animation: 'pulse 2s ease-in-out infinite',
            '@keyframes pulse': {
              '0%, 100%': { opacity: 0.3 },
              '50%': { opacity: 0.6 },
            },
          }}
        >
          scroll to explore ↓
        </Typography>
      </Box>
    </SlideContainer>
  );
}
