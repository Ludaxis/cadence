import { useRef } from 'react';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';

const stages = [
  { icon: '📡', label: 'Events', color: '#00d4ff' },
  { icon: '📊', label: 'Signals', color: '#4fc3f7' },
  { icon: '🔍', label: 'Analysis', color: '#7c4dff' },
  { icon: '🧠', label: 'Modeling', color: '#b388ff' },
  { icon: '🎮', label: 'Adjustment', color: '#ea80fc' },
];

export default function SolutionSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    const tl = gsap.timeline({
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
    });
    tl.from('.pipeline-node', {
      opacity: 0,
      scale: 0.5,
      stagger: 0.12,
      duration: 0.5,
      ease: 'back.out(2)',
    }).from('.pipeline-arrow', {
      opacity: 0,
      scaleX: 0,
      stagger: 0.1,
      duration: 0.3,
    }, '-=0.6');
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="solution">
      <SlideHeader
        number={3}
        title="The Solution Pipeline"
        subtitle="Five stages from raw gameplay to perfect difficulty"
      />

      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 0,
          flexWrap: 'wrap',
          justifyContent: 'center',
        }}
      >
        {stages.map((s, i) => (
          <Box key={s.label} sx={{ display: 'flex', alignItems: 'center' }}>
            <Box
              className="pipeline-node"
              sx={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: 1.5,
              }}
            >
              <Box
                sx={{
                  width: 90,
                  height: 90,
                  borderRadius: '50%',
                  border: `2px solid ${s.color}`,
                  bgcolor: `${s.color}12`,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '2rem',
                  boxShadow: `0 0 24px ${s.color}25`,
                }}
              >
                {s.icon}
              </Box>
              <Typography
                variant="caption"
                sx={{ color: s.color, fontWeight: 600, fontSize: '0.85rem' }}
              >
                {s.label}
              </Typography>
            </Box>

            {i < stages.length - 1 && (
              <Box
                className="pipeline-arrow"
                sx={{
                  width: 60,
                  height: 2,
                  mx: 1,
                  mb: 3,
                  background: `linear-gradient(90deg, ${s.color}, ${stages[i + 1].color})`,
                  borderRadius: 1,
                  transformOrigin: 'left center',
                }}
              />
            )}
          </Box>
        ))}
      </Box>

      <Typography
        variant="body1"
        sx={{ color: 'text.secondary', mt: 5, maxWidth: 600, textAlign: 'center' }}
      >
        Each stage runs in under <strong style={{ color: '#00d4ff' }}>0.5ms</strong> with{' '}
        <strong style={{ color: '#00d4ff' }}>zero garbage collection</strong> pressure.
      </Typography>
    </SlideContainer>
  );
}
