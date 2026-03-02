import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';
import CodeBlock from '../components/CodeBlock';

const phases = [
  { phase: '1', name: 'Install', desc: 'Drop DLL into Plugins/', time: '5 min' },
  { phase: '2', name: 'Initialize', desc: 'One-line DDAEngine.Create()', time: '5 min' },
  { phase: '3', name: 'Emit Signals', desc: 'Add RecordSignal() calls', time: '15 min' },
  { phase: '4', name: 'Apply', desc: 'Read AdjustmentResult', time: '5 min' },
];

const code = `// 1. Initialize
var engine = DDAEngine.Create(config);

// 2. Record gameplay signals
engine.RecordSignal(SignalKey.MoveExecuted);
engine.RecordSignal(SignalKey.ProgressDelta, 0.12f);

// 3. Tick each frame (or on event)
AdjustmentResult result = engine.Tick();

// 4. Apply difficulty
if (result.HasAdjustment)
    ApplyDifficulty(result.NewDifficulty);`;

export default function IntegrationSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    const tl = gsap.timeline({
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
    });
    tl.from('.code-block', { opacity: 0, y: 30, duration: 0.6 })
      .from('.phase-item', { opacity: 0, x: 20, stagger: 0.1, duration: 0.4 }, '-=0.3');
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="integration">
      <SlideHeader
        number={11}
        title="Integration"
        subtitle="From zero to adaptive difficulty in 30 minutes"
      />

      <Box
        sx={{
          display: 'flex',
          gap: 4,
          maxWidth: 1000,
          width: '100%',
          flexDirection: { xs: 'column', md: 'row' },
          alignItems: 'flex-start',
        }}
      >
        <Box className="code-block" sx={{ flex: 1.2, minWidth: 0 }}>
          <CodeBlock code={code} language="csharp" />
        </Box>

        <Box sx={{ flex: 0.8, display: 'flex', flexDirection: 'column', gap: 1.5 }}>
          {phases.map((p) => (
            <Paper
              key={p.phase}
              className="phase-item"
              elevation={0}
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 2,
                p: 2,
                bgcolor: 'rgba(0,212,255,0.04)',
                border: '1px solid rgba(0,212,255,0.1)',
              }}
            >
              <Box
                sx={{
                  width: 32,
                  height: 32,
                  borderRadius: '50%',
                  bgcolor: 'rgba(0,212,255,0.15)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: 'primary.main',
                  fontWeight: 700,
                  fontSize: '0.85rem',
                  flexShrink: 0,
                }}
              >
                {p.phase}
              </Box>
              <Box sx={{ flex: 1 }}>
                <Typography sx={{ color: '#fff', fontWeight: 600, fontSize: '0.85rem' }}>
                  {p.name}
                </Typography>
                <Typography variant="body2" sx={{ color: 'text.secondary', fontSize: '0.78rem' }}>
                  {p.desc}
                </Typography>
              </Box>
              <Typography
                sx={{
                  color: 'primary.main',
                  fontWeight: 600,
                  fontSize: '0.75rem',
                  fontFamily: "'JetBrains Mono', monospace",
                  whiteSpace: 'nowrap',
                }}
              >
                {p.time}
              </Typography>
            </Paper>
          ))}
        </Box>
      </Box>
    </SlideContainer>
  );
}
