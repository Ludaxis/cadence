import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';
import AnimatedCounter from '../components/AnimatedCounter';

const painPoints = [
  { icon: '🐌', title: 'Static Difficulty', desc: 'Levels designed for the average — alienate both ends' },
  { icon: '🎯', title: 'Targets Average', desc: 'One curve for millions of players with different skills' },
  { icon: '⏱️', title: 'Ignores Real-Time', desc: 'No adaptation to frustration, boredom, or flow states' },
];

export default function ProblemSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    gsap.from('.pain-card', {
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
      opacity: 0,
      y: 40,
      stagger: 0.15,
      duration: 0.7,
      ease: 'back.out(1.4)',
    });
  }, { scope: ref });

  return (
    <SlideContainer ref={ref} id="problem">
      <SlideHeader number={2} title="The Problem" subtitle="Why static difficulty kills retention" />

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 5 }}>
        <AnimatedCounter
          end={70}
          suffix="%"
          variant="h1"
          sx={{
            fontSize: { xs: '3rem', md: '5rem' },
            fontWeight: 900,
            color: '#ff5252',
          }}
        />
        <Typography variant="h4" sx={{ color: 'text.secondary' }}>
          of players churn<br />in the first week
        </Typography>
      </Box>

      <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', justifyContent: 'center', maxWidth: 900 }}>
        {painPoints.map((p) => (
          <Paper
            key={p.title}
            className="pain-card"
            elevation={0}
            sx={{
              flex: '1 1 250px',
              maxWidth: 280,
              p: 3,
              bgcolor: 'rgba(255,82,82,0.06)',
              border: '1px solid rgba(255,82,82,0.15)',
              textAlign: 'center',
            }}
          >
            <Typography sx={{ fontSize: '2rem', mb: 1 }}>{p.icon}</Typography>
            <Typography variant="h5" sx={{ color: '#fff', mb: 1 }}>{p.title}</Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>{p.desc}</Typography>
          </Paper>
        ))}
      </Box>
    </SlideContainer>
  );
}
