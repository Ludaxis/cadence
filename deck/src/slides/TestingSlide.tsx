import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';
import AnimatedCounter from '../components/AnimatedCounter';

const tools = [
  {
    name: 'DDA Inspector',
    desc: 'Real-time flow state, rating, and signal visualization in Unity Editor',
    icon: '🔍',
  },
  {
    name: 'Session Replay',
    desc: 'Record and replay player sessions with DDA decisions annotated',
    icon: '🔄',
  },
  {
    name: 'Difficulty Curve Editor',
    desc: 'Visual authoring of difficulty parameters and rule thresholds',
    icon: '📐',
  },
];

export default function TestingSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    const tl = gsap.timeline({
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
    });
    tl.from('.test-counter', { opacity: 0, y: 20, stagger: 0.15, duration: 0.5 })
      .from('.tool-card', { opacity: 0, y: 30, stagger: 0.12, duration: 0.5 }, '-=0.3');
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="testing">
      <SlideHeader
        number={13}
        title="Testing & Tools"
        subtitle="Comprehensive test coverage and editor-time debugging"
      />

      <Box sx={{ display: 'flex', gap: 5, mb: 5, flexWrap: 'wrap', justifyContent: 'center' }}>
        <Box className="test-counter" sx={{ textAlign: 'center' }}>
          <AnimatedCounter end={52} variant="h2" sx={{ color: '#00e676', fontWeight: 800 }} />
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>Unit Tests</Typography>
        </Box>
        <Box className="test-counter" sx={{ textAlign: 'center' }}>
          <AnimatedCounter end={8} variant="h2" sx={{ color: '#4fc3f7', fontWeight: 800 }} />
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>Integration Tests</Typography>
        </Box>
        <Box className="test-counter" sx={{ textAlign: 'center' }}>
          <AnimatedCounter end={3} variant="h2" sx={{ color: '#b388ff', fontWeight: 800 }} />
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>Editor Tools</Typography>
        </Box>
      </Box>

      <Box
        sx={{
          display: 'flex',
          gap: 2.5,
          flexWrap: 'wrap',
          justifyContent: 'center',
          maxWidth: 900,
        }}
      >
        {tools.map((t) => (
          <Paper
            key={t.name}
            className="tool-card"
            elevation={0}
            sx={{
              flex: '1 1 250px',
              maxWidth: 280,
              p: 3,
              bgcolor: 'rgba(179,136,255,0.05)',
              border: '1px solid rgba(179,136,255,0.15)',
              textAlign: 'center',
            }}
          >
            <Typography sx={{ fontSize: '2rem', mb: 1 }}>{t.icon}</Typography>
            <Typography variant="h5" sx={{ color: '#fff', mb: 1, fontSize: '1rem' }}>
              {t.name}
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary', fontSize: '0.82rem' }}>
              {t.desc}
            </Typography>
          </Paper>
        ))}
      </Box>
    </SlideContainer>
  );
}
