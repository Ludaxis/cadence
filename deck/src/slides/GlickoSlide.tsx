import { useRef } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';
import SlideContainer from '../components/SlideContainer';
import SlideHeader from '../components/SlideHeader';

const components = [
  { name: 'Rating (μ)', value: '1500', desc: 'Player skill estimate', color: '#00d4ff' },
  { name: 'Deviation (φ)', value: '350→50', desc: 'Uncertainty range', color: '#7c4dff' },
  { name: 'Volatility (σ)', value: '0.06', desc: 'Consistency of play', color: '#b388ff' },
];

const behaviors = [
  { session: 'Early (1-5)', desc: 'Large swings, fast convergence', deviation: 'High' },
  { session: 'Mid (6-15)', desc: 'Stabilizing, reliable predictions', deviation: 'Medium' },
  { session: 'Late (15+)', desc: 'Fine-grained, low noise', deviation: 'Low' },
];

export default function GlickoSlide() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(() => {
    const tl = gsap.timeline({
      scrollTrigger: { trigger: ref.current, start: 'top 60%', once: true },
    });
    tl.from('.glicko-stat', {
      opacity: 0,
      y: 30,
      stagger: 0.15,
      duration: 0.6,
      ease: 'power2.out',
    }).from('.glicko-chart-bar', {
      scaleX: 0,
      transformOrigin: 'left center',
      stagger: 0.08,
      duration: 0.5,
      ease: 'power2.out',
    }, '-=0.3')
    .from('.behavior-row', {
      opacity: 0,
      x: 20,
      stagger: 0.1,
      duration: 0.4,
    }, '-=0.3');
  }, { scope: ref });

  // Simple rating curve data (20 sessions)
  const ratingCurve = [
    1500, 1420, 1380, 1450, 1480, 1520, 1510, 1550, 1580, 1560,
    1600, 1620, 1610, 1640, 1660, 1650, 1670, 1680, 1690, 1700,
  ];
  const maxR = Math.max(...ratingCurve);
  const minR = Math.min(...ratingCurve);

  return (
    <SlideContainer ref={ref} id="glicko">
      <SlideHeader
        number={8}
        title="Glicko-2 Player Model"
        subtitle="Bayesian skill estimation adapted for single-player puzzle games"
      />

      {/* Three components */}
      <Box sx={{ display: 'flex', gap: 3, mb: 4, flexWrap: 'wrap', justifyContent: 'center' }}>
        {components.map((c) => (
          <Paper
            key={c.name}
            className="glicko-stat"
            elevation={0}
            sx={{
              px: 3,
              py: 2,
              bgcolor: `${c.color}08`,
              border: `1px solid ${c.color}25`,
              textAlign: 'center',
              minWidth: 160,
            }}
          >
            <Typography sx={{ color: c.color, fontWeight: 700, fontSize: '0.85rem' }}>
              {c.name}
            </Typography>
            <Typography variant="h4" sx={{ color: '#fff', fontWeight: 800, my: 0.5 }}>
              {c.value}
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary', fontSize: '0.78rem' }}>
              {c.desc}
            </Typography>
          </Paper>
        ))}
      </Box>

      {/* Rating curve visualization */}
      <Box sx={{ maxWidth: 700, width: '100%', mb: 4 }}>
        <Typography variant="h6" sx={{ color: '#fff', fontSize: '0.85rem', mb: 1.5 }}>
          Rating convergence over 20 sessions
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'flex-end', gap: '3px', height: 80 }}>
          {ratingCurve.map((r, i) => (
            <Box
              key={i}
              className="glicko-chart-bar"
              sx={{
                flex: 1,
                height: `${((r - minR) / (maxR - minR)) * 100}%`,
                minHeight: 4,
                bgcolor: i < 5 ? '#00d4ff' : i < 15 ? '#7c4dff' : '#b388ff',
                borderRadius: '2px 2px 0 0',
                opacity: 0.8,
              }}
            />
          ))}
        </Box>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 0.5 }}>
          <Typography sx={{ fontSize: '0.65rem', color: 'text.secondary' }}>Session 1</Typography>
          <Typography sx={{ fontSize: '0.65rem', color: 'text.secondary' }}>Session 20</Typography>
        </Box>
      </Box>

      {/* Behavior table */}
      <Box sx={{ maxWidth: 600, width: '100%' }}>
        {behaviors.map((b) => (
          <Box
            key={b.session}
            className="behavior-row"
            sx={{
              display: 'flex',
              gap: 2,
              py: 1,
              borderBottom: '1px solid rgba(255,255,255,0.06)',
              alignItems: 'center',
            }}
          >
            <Typography
              sx={{
                fontFamily: "'JetBrains Mono', monospace",
                fontSize: '0.78rem',
                color: 'primary.main',
                minWidth: 100,
              }}
            >
              {b.session}
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary', flex: 1, fontSize: '0.82rem' }}>
              {b.desc}
            </Typography>
            <Typography
              sx={{
                fontSize: '0.72rem',
                color: b.deviation === 'High' ? '#ff9100' : b.deviation === 'Medium' ? '#ffd740' : '#00e676',
                fontWeight: 600,
              }}
            >
              {b.deviation} φ
            </Typography>
          </Box>
        ))}
      </Box>
    </SlideContainer>
  );
}
