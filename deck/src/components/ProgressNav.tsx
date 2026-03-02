import { useEffect, useState } from 'react';
import Box from '@mui/material/Box';

const SLIDE_IDS = [
  'title', 'problem', 'solution', 'architecture', 'signals',
  'gameplay-signals', 'flow', 'glicko', 'adjustment', 'catalog',
  'integration', 'performance', 'testing', 'timeline', 'summary',
];

export default function ProgressNav() {
  const [active, setActive] = useState(0);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const idx = SLIDE_IDS.indexOf(entry.target.id);
            if (idx >= 0) setActive(idx);
          }
        });
      },
      { threshold: 0.5 },
    );

    SLIDE_IDS.forEach((id) => {
      const el = document.getElementById(id);
      if (el) observer.observe(el);
    });

    return () => observer.disconnect();
  }, []);

  const scrollTo = (id: string) => {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <Box
      sx={{
        position: 'fixed',
        right: 24,
        top: '50%',
        transform: 'translateY(-50%)',
        display: 'flex',
        flexDirection: 'column',
        gap: '10px',
        zIndex: 1000,
      }}
    >
      {SLIDE_IDS.map((id, i) => (
        <Box
          key={id}
          onClick={() => scrollTo(id)}
          sx={{
            width: active === i ? 12 : 8,
            height: active === i ? 12 : 8,
            borderRadius: '50%',
            bgcolor: active === i ? 'primary.main' : 'rgba(255,255,255,0.25)',
            cursor: 'pointer',
            transition: 'all 0.3s ease',
            boxShadow: active === i ? '0 0 8px #00d4ff' : 'none',
            '&:hover': {
              bgcolor: active === i ? 'primary.main' : 'rgba(255,255,255,0.5)',
              transform: 'scale(1.3)',
            },
          }}
        />
      ))}
    </Box>
  );
}
