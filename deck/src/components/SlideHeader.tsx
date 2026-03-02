import Box from '@mui/material/Box';
import Chip from '@mui/material/Chip';
import Typography from '@mui/material/Typography';

interface Props {
  number: number;
  title: string;
  subtitle?: string;
}

export default function SlideHeader({ number, title, subtitle }: Props) {
  return (
    <Box sx={{ textAlign: 'center', mb: 5 }}>
      <Chip
        label={`${String(number).padStart(2, '0')} / 15`}
        size="small"
        sx={{
          mb: 2,
          bgcolor: 'rgba(0,212,255,0.12)',
          color: 'primary.main',
          fontSize: '0.75rem',
          border: '1px solid rgba(0,212,255,0.25)',
        }}
      />
      <Typography variant="h2" sx={{ color: '#fff', mb: 1 }}>
        {title}
      </Typography>
      {subtitle && (
        <Typography variant="body1" sx={{ color: 'text.secondary', maxWidth: 600, mx: 'auto' }}>
          {subtitle}
        </Typography>
      )}
    </Box>
  );
}
