import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Chip from '@mui/material/Chip';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';

const rules = [
  {
    name: 'FlowChannel',
    icon: '🌊',
    desc: 'Keeps difficulty in the optimal challenge band around player rating',
    trigger: 'Always active',
    color: '#00e676',
  },
  {
    name: 'StreakDamper',
    icon: '🎢',
    desc: 'Prevents win/loss streaks from over-adjusting difficulty',
    trigger: 'After 3+ consecutive same outcomes',
    color: '#ffd740',
  },
  {
    name: 'FrustrationRelief',
    icon: '🛟',
    desc: 'Immediate difficulty reduction when frustration state is detected',
    trigger: 'FlowState == Frustration',
    color: '#ff5252',
  },
  {
    name: 'Cooldown',
    icon: '❄️',
    desc: 'Minimum interval between adjustments to prevent oscillation',
    trigger: 'Time since last adjustment < threshold',
    color: '#4fc3f7',
  },
];

export default function AdjustmentSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    gsap.from('.rule-card', {
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
      opacity: 0,
      x: -40,
      stagger: 0.15,
      duration: 0.6,
      ease: 'power2.out',
    });
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="adjustment">
      <SlideHeader
        number={9}
        title="Adjustment Rules"
        subtitle="Four rules govern how and when difficulty changes"
      />

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 750, width: '100%' }}>
        {rules.map((r, i) => (
          <Paper
            key={r.name}
            className="rule-card"
            elevation={0}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 2.5,
              p: 2.5,
              bgcolor: `${r.color}06`,
              border: `1px solid ${r.color}20`,
            }}
          >
            <Typography sx={{ fontSize: '2rem' }}>{r.icon}</Typography>
            <Box sx={{ flex: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                <Typography sx={{ color: '#fff', fontWeight: 700, fontSize: '1rem' }}>
                  {r.name}
                </Typography>
                <Chip
                  label={`Rule ${i + 1}`}
                  size="small"
                  sx={{
                    height: 20,
                    fontSize: '0.65rem',
                    bgcolor: `${r.color}20`,
                    color: r.color,
                  }}
                />
              </Box>
              <Typography variant="body2" sx={{ color: 'text.secondary', mb: 0.5, fontSize: '0.85rem' }}>
                {r.desc}
              </Typography>
              <Typography
                sx={{
                  fontFamily: "'JetBrains Mono', monospace",
                  fontSize: '0.72rem',
                  color: r.color,
                  opacity: 0.7,
                }}
              >
                Trigger: {r.trigger}
              </Typography>
            </Box>
          </Paper>
        ))}
      </Box>
    </SlideContainer>
  );
}
