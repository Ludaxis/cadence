import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Chip from '@mui/material/Chip';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';

const priorities = [
  {
    level: 'P0',
    name: 'Critical',
    color: '#ff5252',
    count: 4,
    events: ['session.start', 'session.end', 'level.start', 'level.complete'],
  },
  {
    level: 'P1',
    name: 'Core',
    color: '#ff9100',
    count: 8,
    events: ['move.executed', 'move.optimal', 'booster.used', 'difficulty.adjusted'],
  },
  {
    level: 'P2',
    name: 'Diagnostic',
    color: '#ffd740',
    count: 12,
    events: ['flow.state.changed', 'rating.updated', 'streak.detected', 'frustration.relief'],
  },
  {
    level: 'P3',
    name: 'Analytical',
    color: '#4fc3f7',
    count: 10,
    events: ['window.snapshot', 'behavior.profile', 'tempo.analysis'],
  },
  {
    level: 'P4',
    name: 'Debug',
    color: '#9e9e9e',
    count: 6,
    events: ['signal.raw', 'rule.evaluated', 'cooldown.active'],
  },
];

export default function EventCatalogSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    gsap.from('.priority-row', {
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
      opacity: 0,
      y: 20,
      stagger: 0.1,
      duration: 0.5,
      ease: 'power2.out',
    });
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="catalog">
      <SlideHeader
        number={10}
        title="Event Catalog"
        subtitle="40 structured analytics events across 5 priority levels"
      />

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, maxWidth: 800, width: '100%' }}>
        {priorities.map((p) => (
          <Paper
            key={p.level}
            className="priority-row"
            elevation={0}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 2,
              p: 2,
              bgcolor: `${p.color}06`,
              border: `1px solid ${p.color}15`,
            }}
          >
            <Chip
              label={p.level}
              size="small"
              sx={{
                bgcolor: `${p.color}20`,
                color: p.color,
                fontWeight: 700,
                fontFamily: "'JetBrains Mono', monospace",
                minWidth: 40,
              }}
            />
            <Typography sx={{ color: p.color, fontWeight: 600, minWidth: 90, fontSize: '0.85rem' }}>
              {p.name}
            </Typography>
            <Chip
              label={`${p.count} events`}
              size="small"
              variant="outlined"
              sx={{
                borderColor: `${p.color}30`,
                color: 'text.secondary',
                fontSize: '0.72rem',
                height: 22,
              }}
            />
            <Box sx={{ display: 'flex', gap: 0.8, flexWrap: 'wrap', flex: 1 }}>
              {p.events.map((e) => (
                <Typography
                  key={e}
                  sx={{
                    fontFamily: "'JetBrains Mono', monospace",
                    fontSize: '0.7rem',
                    color: 'text.secondary',
                    bgcolor: 'rgba(255,255,255,0.04)',
                    px: 0.8,
                    py: 0.2,
                    borderRadius: 0.5,
                  }}
                >
                  {e}
                </Typography>
              ))}
            </Box>
          </Paper>
        ))}
      </Box>
    </SlideContainer>
  );
}
