import { useRef, ComponentProps } from 'react';
import Typography from '@mui/material/Typography';
import { useGSAP } from '@gsap/react';
import gsap from 'gsap';

interface Props extends Omit<ComponentProps<typeof Typography>, 'children'> {
  end: number;
  prefix?: string;
  suffix?: string;
  decimals?: number;
  duration?: number;
}

export default function AnimatedCounter({
  end,
  prefix = '',
  suffix = '',
  decimals = 0,
  duration = 1.5,
  ...typoProps
}: Props) {
  const ref = useRef<HTMLSpanElement>(null);
  const counter = useRef({ count: 0 });

  useGSAP(() => {
    gsap.to(counter.current, {
      count: end,
      duration,
      ease: 'power2.out',
      snap: decimals === 0 ? { count: 1 } : undefined,
      scrollTrigger: {
        trigger: ref.current,
        start: 'top 85%',
        once: true,
      },
      onUpdate: () => {
        if (ref.current) {
          ref.current.textContent =
            prefix + counter.current.count.toFixed(decimals) + suffix;
        }
      },
    });
  }, { scope: ref });

  return (
    <Typography ref={ref} {...typoProps}>
      {prefix}0{suffix}
    </Typography>
  );
}
