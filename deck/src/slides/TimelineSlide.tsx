import { useRef } from 'react';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';

const phases = [
  {
    week: 'Week 1',
    title: 'Signal Infrastructure',
    items: ['SignalBus + SignalDefinition', 'Tier system (T0-T4)', 'SlidingWindow analytics'],
    color: '#00d4ff',
  },
  {
    week: 'Week 2',
    title: 'Player Model & Flow',
    items: ['Glicko-2 implementation', 'FlowStateDetector', 'Hysteresis guards'],
    color: '#7c4dff',
  },
  {
    week: 'Week 3',
    title: 'Adjustment Engine',
    items: ['Rule evaluator pipeline', 'FlowChannel + StreakDamper', 'FrustrationRelief + Cooldown'],
    color: '#b388ff',
  },
  {
    week: 'Week 4',
    title: 'Polish & Tools',
    items: ['Editor inspector tools', 'Event catalog + analytics', '60 tests + documentation'],
    color: '#ea80fc',
  },
];

export default function TimelineSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    gsap.from('.timeline-phase', {
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
      opacity: 0,
      y: 40,
      stagger: 0.15,
      duration: 0.6,
      ease: 'power2.out',
    });
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="timeline">
      <SlideHeader
        number={14}
        title="Implementation Timeline"
        subtitle="4 weeks from first commit to production-ready SDK"
      />

      <Box sx={{ position: 'relative', maxWidth: 600, width: '100%' }}>
        {/* Vertical line */}
        <Box
          sx={{
            position: 'absolute',
            left: 20,
            top: 0,
            bottom: 0,
            width: 2,
            background: 'linear-gradient(180deg, #00d4ff, #ea80fc)',
            opacity: 0.3,
          }}
        />

        {phases.map((p) => (
          <Box
            key={p.week}
            className="timeline-phase"
            sx={{
              display: 'flex',
              gap: 3,
              mb: 3,
              position: 'relative',
            }}
          >
            {/* Dot */}
            <Box
              sx={{
                width: 42,
                height: 42,
                borderRadius: '50%',
                border: `2px solid ${p.color}`,
                bgcolor: `${p.color}15`,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                flexShrink: 0,
                zIndex: 1,
              }}
            >
              <Typography sx={{ color: p.color, fontWeight: 700, fontSize: '0.7rem' }}>
                {p.week.replace('Week ', 'W')}
              </Typography>
            </Box>

            <Box sx={{ pt: 0.5 }}>
              <Typography sx={{ color: p.color, fontWeight: 600, fontSize: '0.8rem', mb: 0.3 }}>
                {p.week}
              </Typography>
              <Typography sx={{ color: '#fff', fontWeight: 700, fontSize: '1.05rem', mb: 1 }}>
                {p.title}
              </Typography>
              {p.items.map((item) => (
                <Typography
                  key={item}
                  variant="body2"
                  sx={{
                    color: 'text.secondary',
                    fontSize: '0.82rem',
                    pl: 1.5,
                    position: 'relative',
                    mb: 0.3,
                    '&::before': {
                      content: '"→"',
                      position: 'absolute',
                      left: 0,
                      color: p.color,
                      opacity: 0.5,
                    },
                  }}
                >
                  {item}
                </Typography>
              ))}
            </Box>
          </Box>
        ))}
      </Box>
    </SlideContainer>
  );
}
