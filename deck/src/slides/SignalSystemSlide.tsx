import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Chip from '@mui/material/Chip';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';

const tiers = [
  { tier: 'T0', name: 'Core', color: '#00d4ff', keys: ['move.executed', 'level.start', 'level.end'] },
  { tier: 'T1', name: 'Quality', color: '#4fc3f7', keys: ['move.optimal', 'move.waste', 'progress.delta'] },
  { tier: 'T2', name: 'Temporal', color: '#7c4dff', keys: ['tempo.pause', 'tempo.burst', 'session.duration'] },
  { tier: 'T3', name: 'Behavioral', color: '#b388ff', keys: ['input.rejected', 'undo.used', 'hint.used'] },
  { tier: 'T4', name: 'Derived', color: '#ea80fc', keys: ['efficiency.ratio', 'streak.length', 'flow.score'] },
];

export default function SignalSystemSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    gsap.from('.tier-card', {
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
      opacity: 0,
      x: -50,
      stagger: 0.1,
      duration: 0.5,
      ease: 'power2.out',
    });
    gsap.from('.signal-key', {
      scrollTrigger: { trigger: ref.current, start: 'top 50%', once: true },
      opacity: 0,
      stagger: 0.03,
      duration: 0.3,
      delay: 0.5,
    });
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="signals">
      <SlideHeader
        number={5}
        title="Signal Tier System"
        subtitle="Five tiers of gameplay telemetry, from raw events to derived insights"
      />

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 800, width: '100%' }}>
        {tiers.map((t) => (
          <Paper
            key={t.tier}
            className="tier-card"
            elevation={0}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 2,
              p: 2,
              bgcolor: `${t.color}08`,
              border: `1px solid ${t.color}20`,
              borderLeft: `4px solid ${t.color}`,
            }}
          >
            <Chip
              label={t.tier}
              size="small"
              sx={{
                bgcolor: `${t.color}20`,
                color: t.color,
                fontWeight: 700,
                minWidth: 45,
                fontFamily: "'JetBrains Mono', monospace",
              }}
            />
            <Typography sx={{ color: t.color, fontWeight: 600, minWidth: 90, fontSize: '0.9rem' }}>
              {t.name}
            </Typography>
            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
              {t.keys.map((k) => (
                <Typography
                  key={k}
                  className="signal-key"
                  sx={{
                    fontFamily: "'JetBrains Mono', monospace",
                    fontSize: '0.75rem',
                    color: 'text.secondary',
                    bgcolor: 'rgba(255,255,255,0.04)',
                    px: 1,
                    py: 0.3,
                    borderRadius: 1,
                  }}
                >
                  {k}
                </Typography>
              ))}
            </Box>
          </Paper>
        ))}
      </Box>
    </SlideContainer>
  );
}
