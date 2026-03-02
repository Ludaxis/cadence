import { useEffect } from 'react';
import gsap from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import ProgressNav from './components/ProgressNav';
import TitleSlide from './slides/TitleSlide';
import ProblemSlide from './slides/ProblemSlide';
import SolutionSlide from './slides/SolutionSlide';
import ArchitectureSlide from './slides/ArchitectureSlide';
import SignalSystemSlide from './slides/SignalSystemSlide';
import GameplaySignalsSlide from './slides/GameplaySignalsSlide';
import FlowDetectionSlide from './slides/FlowDetectionSlide';
import GlickoSlide from './slides/GlickoSlide';
import AdjustmentSlide from './slides/AdjustmentSlide';
import EventCatalogSlide from './slides/EventCatalogSlide';
import IntegrationSlide from './slides/IntegrationSlide';
import PerformanceSlide from './slides/PerformanceSlide';
import TestingSlide from './slides/TestingSlide';
import TimelineSlide from './slides/TimelineSlide';
import SummarySlide from './slides/SummarySlide';

gsap.registerPlugin(ScrollTrigger);

const SLIDE_IDS = [
  'title', 'problem', 'solution', 'architecture', 'signals',
  'gameplay-signals', 'flow', 'glicko', 'adjustment', 'catalog',
  'integration', 'performance', 'testing', 'timeline', 'summary',
];

export default function App() {
  useEffect(() => {
    const handleKey = (e: KeyboardEvent) => {
      const slides = SLIDE_IDS.map((id) => document.getElementById(id)).filter(Boolean) as HTMLElement[];
      const scrollY = window.scrollY;
      const vh = window.innerHeight;

      let current = 0;
      for (let i = 0; i < slides.length; i++) {
        if (slides[i].offsetTop <= scrollY + vh * 0.5) {
          current = i;
        }
      }

      if (e.key === 'ArrowDown' || e.key === ' ') {
        e.preventDefault();
        const next = Math.min(current + 1, slides.length - 1);
        slides[next].scrollIntoView({ behavior: 'smooth' });
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        const prev = Math.max(current - 1, 0);
        slides[prev].scrollIntoView({ behavior: 'smooth' });
      }
    };

    window.addEventListener('keydown', handleKey);
    return () => window.removeEventListener('keydown', handleKey);
  }, []);

  return (
    <>
      <ProgressNav />
      <TitleSlide />
      <ProblemSlide />
      <SolutionSlide />
      <ArchitectureSlide />
      <SignalSystemSlide />
      <GameplaySignalsSlide />
      <FlowDetectionSlide />
      <GlickoSlide />
      <AdjustmentSlide />
      <EventCatalogSlide />
      <IntegrationSlide />
      <PerformanceSlide />
      <TestingSlide />
      <TimelineSlide />
      <SummarySlide />
    </>
  );
}
