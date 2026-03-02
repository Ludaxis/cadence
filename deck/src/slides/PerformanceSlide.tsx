import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';
import AnimatedCounter from '../components/AnimatedCounter';

const metrics = [
  { label: 'RecordSignal()', value: 0.01, suffix: 'ms', prefix: '<', decimals: 2, color: '#00e676', desc: 'Per signal recording' },
  { label: 'Tick()', value: 0.5, suffix: 'ms', prefix: '<', decimals: 1, color: '#00d4ff', desc: 'Full pipeline per frame' },
  { label: 'GC Alloc', value: 0, suffix: ' bytes', prefix: '', decimals: 0, color: '#b388ff', desc: 'Zero garbage per tick' },
  { label: 'Runtime RAM', value: 50, suffix: 'KB', prefix: '<', decimals: 0, color: '#ffd740', desc: 'Total memory footprint' },
  { label: 'Binary Size', value: 200, suffix: 'KB', prefix: '<', decimals: 0, color: '#ea80fc', desc: 'DLL + config assets' },
];

export default function PerformanceSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    gsap.from('.perf-card', {
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
      opacity: 0,
      y: 30,
      stagger: 0.1,
      duration: 0.5,
      ease: 'power2.out',
    });
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="performance">
      <SlideHeader
        number={12}
        title="Performance"
        subtitle="Built for mobile — zero allocation, sub-millisecond, tiny footprint"
      />

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', md: 'repeat(3, 1fr)' },
          gap: 2.5,
          maxWidth: 850,
          width: '100%',
        }}
      >
        {metrics.map((m) => (
          <Paper
            key={m.label}
            className="perf-card"
            elevation={0}
            sx={{
              p: 3,
              textAlign: 'center',
              bgcolor: `${m.color}06`,
              border: `1px solid ${m.color}18`,
            }}
          >
            <AnimatedCounter
              end={m.value}
              prefix={m.prefix}
              suffix={m.suffix}
              decimals={m.decimals}
              variant="h3"
              sx={{ color: m.color, fontWeight: 800, mb: 0.5 }}
            />
            <Typography sx={{ color: '#fff', fontWeight: 600, fontSize: '0.9rem', mb: 0.5 }}>
              {m.label}
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary', fontSize: '0.78rem' }}>
              {m.desc}
            </Typography>
          </Paper>
        ))}
      </Box>
    </SlideContainer>
  );
}
