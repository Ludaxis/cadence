import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';

const groups = [
  {
    title: 'Signal Layer',
    color: '#00d4ff',
    items: ['SignalBus', 'SignalDefinition', 'DerivedSignalProcessor'],
  },
  {
    title: 'Analysis',
    color: '#4fc3f7',
    items: ['SlidingWindow', 'PerformanceAnalyzer', 'TempoTracker'],
  },
  {
    title: 'Player Model',
    color: '#7c4dff',
    items: ['Glicko2Rating', 'SessionHistory', 'BehaviorProfile'],
  },
  {
    title: 'Flow Detection',
    color: '#b388ff',
    items: ['FlowStateDetector', 'HysteresisGuard', 'EmotionVector'],
  },
  {
    title: 'Adjustment Engine',
    color: '#ea80fc',
    items: ['RuleEvaluator', 'AdjustmentPipeline', 'CooldownManager'],
  },
];

export default function ArchitectureSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    gsap.from('.arch-group', {
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
      opacity: 0,
      x: -30,
      stagger: 0.12,
      duration: 0.6,
      ease: 'power2.out',
    });
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="architecture">
      <SlideHeader
        number={4}
        title="Architecture"
        subtitle="Five component groups, loosely coupled via interfaces"
      />

      <Box
        sx={{
          display: 'flex',
          gap: 2.5,
          flexWrap: 'wrap',
          justifyContent: 'center',
          maxWidth: 1100,
        }}
      >
        {groups.map((g) => (
          <Paper
            key={g.title}
            className="arch-group"
            elevation={0}
            sx={{
              flex: '1 1 180px',
              maxWidth: 200,
              p: 2.5,
              bgcolor: `${g.color}08`,
              border: `1px solid ${g.color}30`,
              borderTop: `3px solid ${g.color}`,
            }}
          >
            <Typography
              variant="h6"
              sx={{ color: g.color, fontSize: '0.85rem', fontWeight: 700, mb: 1.5 }}
            >
              {g.title}
            </Typography>
            {g.items.map((item) => (
              <Typography
                key={item}
                variant="body2"
                sx={{
                  color: 'text.secondary',
                  fontSize: '0.78rem',
                  fontFamily: "'JetBrains Mono', monospace",
                  mb: 0.5,
                }}
              >
                {item}
              </Typography>
            ))}
          </Paper>
        ))}
      </Box>

      {/* Connection arrows */}
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3, gap: 1 }}>
        {groups.slice(0, -1).map((g, i) => (
          <Box key={i} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Box
              sx={{
                width: 40,
                height: 2,
                background: `linear-gradient(90deg, ${g.color}, ${groups[i + 1].color})`,
              }}
            />
            <Typography sx={{ color: groups[i + 1].color, fontSize: '0.7rem' }}>→</Typography>
          </Box>
        ))}
      </Box>
    </SlideContainer>
  );
}
