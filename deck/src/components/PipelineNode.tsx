import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';

interface Props {
  label: string;
  icon?: string;
  color?: string;
  size?: number;
  className?: string;
}

export default function PipelineNode({ label, icon, color = '#00d4ff', size = 80, className }: Props) {
  return (
    <Box
      className={className}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 1,
      }}
    >
      <Box
        sx={{
          width: size,
          height: size,
          borderRadius: '50%',
          border: `2px solid ${color}`,
          bgcolor: `${color}15`,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: size * 0.35,
          boxShadow: `0 0 20px ${color}30`,
        }}
      >
        {icon || '●'}
      </Box>
      <Typography
        variant="caption"
        sx={{ color, fontWeight: 600, fontSize: '0.75rem', textAlign: 'center', maxWidth: size + 20 }}
      >
        {label}
      </Typography>
    </Box>
  );
}
