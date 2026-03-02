import { forwardRef, ReactNode } from 'react';
import Box from '@mui/material/Box';

interface Props {
  children: ReactNode;
  id?: string;
  bg?: string;
}

const SlideContainer = forwardRef<HTMLDivElement, Props>(({ children, id, bg }, ref) => (
  <Box
    ref={ref}
    id={id}
    sx={{
      minHeight: '100vh',
      width: '100%',
      scrollSnapAlign: 'start',
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      px: { xs: 3, md: 8 },
      py: 6,
      position: 'relative',
      overflow: 'hidden',
      background: bg || 'transparent',
    }}
  >
    {children}
  </Box>
));

SlideContainer.displayName = 'SlideContainer';
export default SlideContainer;
