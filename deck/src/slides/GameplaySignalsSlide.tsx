import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';

const signals = [
  { key: 'move.executed', icon: '🎯', desc: 'Player performed a game action', tier: 'T0' },
  { key: 'move.optimal', icon: '✨', desc: 'Action matched the ideal solution', tier: 'T1' },
  { key: 'move.waste', icon: '💨', desc: 'Action had no positive effect', tier: 'T1' },
  { key: 'progress.delta', icon: '📈', desc: 'Change in completion percentage', tier: 'T1' },
  { key: 'tempo.pause', icon: '⏸️', desc: 'Player hesitated beyond threshold', tier: 'T2' },
  { key: 'input.rejected', icon: '🚫', desc: 'Attempted an invalid action', tier: 'T3' },
];

export default function GameplaySignalsSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    gsap.from('.signal-card', {
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
      opacity: 0,
      rotateY: 90,
      stagger: 0.1,
      duration: 0.6,
      ease: 'power2.out',
    });
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="gameplay-signals">
      <SlideHeader
        number={6}
        title="6 Core Gameplay Signals"
        subtitle="The essential signals every puzzle game should emit"
      />

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: '1fr 1fr 1fr' },
          gap: 2.5,
          maxWidth: 900,
          width: '100%',
        }}
      >
        {signals.map((s) => (
          <Paper
            key={s.key}
            className="signal-card"
            elevation={0}
            sx={{
              p: 2.5,
              bgcolor: 'rgba(0,212,255,0.04)',
              border: '1px solid rgba(0,212,255,0.12)',
              textAlign: 'center',
              transition: 'border-color 0.3s',
              '&:hover': { borderColor: 'rgba(0,212,255,0.4)' },
            }}
          >
            <Typography sx={{ fontSize: '2rem', mb: 1 }}>{s.icon}</Typography>
            <Typography
              sx={{
                fontFamily: "'JetBrains Mono', monospace",
                fontSize: '0.82rem',
                color: 'primary.main',
                fontWeight: 600,
                mb: 1,
              }}
            >
              {s.key}
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary', fontSize: '0.82rem' }}>
              {s.desc}
            </Typography>
            <Typography
              sx={{
                mt: 1,
                fontSize: '0.65rem',
                color: 'rgba(255,255,255,0.3)',
                fontWeight: 600,
              }}
            >
              {s.tier}
            </Typography>
          </Paper>
        ))}
      </Box>
    </SlideContainer>
  );
}
