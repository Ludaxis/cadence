import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';

const states = [
  { name: 'Flow', color: '#00e676', emoji: '🟢', desc: 'Optimal challenge-skill balance' },
  { name: 'Boredom', color: '#ffd740', emoji: '🟡', desc: 'Challenge too low for skill' },
  { name: 'Anxiety', color: '#ff9100', emoji: '🟠', desc: 'Challenge too high for skill' },
  { name: 'Frustration', color: '#ff5252', emoji: '🔴', desc: 'Repeated failure + low progress' },
];

const windows = [
  { name: 'Micro', span: '10 moves', desc: 'Immediate reaction' },
  { name: 'Meso', span: '1 level', desc: 'Level-scale pattern' },
  { name: 'Macro', span: '5 sessions', desc: 'Long-term trend' },
];

export default function FlowDetectionSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    const tl = gsap.timeline({
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
    });
    tl.from('.flow-state', {
      opacity: 0,
      scale: 0.6,
      stagger: 0.12,
      duration: 0.5,
      ease: 'back.out(2)',
    }).from('.window-card', {
      opacity: 0,
      y: 30,
      stagger: 0.1,
      duration: 0.5,
    }, '-=0.3');
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="flow">
      <SlideHeader
        number={7}
        title="Flow Detection"
        subtitle="Real-time emotional state classification with hysteresis guards"
      />

      {/* State circles */}
      <Box sx={{ display: 'flex', gap: 4, mb: 5, flexWrap: 'wrap', justifyContent: 'center' }}>
        {states.map((s) => (
          <Box
            key={s.name}
            className="flow-state"
            sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1 }}
          >
            <Box
              sx={{
                width: 80,
                height: 80,
                borderRadius: '50%',
                border: `2px solid ${s.color}`,
                bgcolor: `${s.color}15`,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: '1.8rem',
                boxShadow: `0 0 20px ${s.color}30`,
                animation: s.name === 'Flow' ? 'flowPulse 2s ease-in-out infinite' : 'none',
                '@keyframes flowPulse': {
                  '0%, 100%': { boxShadow: `0 0 20px ${s.color}30` },
                  '50%': { boxShadow: `0 0 35px ${s.color}50` },
                },
              }}
            >
              {s.emoji}
            </Box>
            <Typography sx={{ color: s.color, fontWeight: 700, fontSize: '0.9rem' }}>
              {s.name}
            </Typography>
            <Typography
              variant="body2"
              sx={{ color: 'text.secondary', fontSize: '0.75rem', textAlign: 'center', maxWidth: 130 }}
            >
              {s.desc}
            </Typography>
          </Box>
        ))}
      </Box>

      {/* Sliding windows */}
      <Typography variant="h5" sx={{ color: '#fff', mb: 2 }}>
        3 Sliding Windows
      </Typography>
      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', justifyContent: 'center' }}>
        {windows.map((w) => (
          <Paper
            key={w.name}
            className="window-card"
            elevation={0}
            sx={{
              px: 3,
              py: 2,
              bgcolor: 'rgba(179,136,255,0.06)',
              border: '1px solid rgba(179,136,255,0.2)',
              textAlign: 'center',
              minWidth: 150,
            }}
          >
            <Typography sx={{ color: '#b388ff', fontWeight: 700, fontSize: '1rem' }}>
              {w.name}
            </Typography>
            <Typography
              sx={{
                fontFamily: "'JetBrains Mono', monospace",
                fontSize: '0.8rem',
                color: 'text.secondary',
              }}
            >
              {w.span}
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary', fontSize: '0.75rem', mt: 0.5 }}>
              {w.desc}
            </Typography>
          </Paper>
        ))}
      </Box>
    </SlideContainer>
  );
}
