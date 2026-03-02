import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';

interface Props {
  code: string;
  language?: string;
}

const keywords = ['using', 'var', 'new', 'void', 'public', 'private', 'class', 'protected', 'override', 'static', 'readonly', 'const', 'return', 'if', 'else', 'true', 'false', 'null', 'this', 'async', 'await'];
const types = ['DDAEngine', 'SignalKey', 'AdjustmentResult', 'FlowState', 'PlayerModel', 'GameplaySignals', 'SignalTier', 'string', 'int', 'float', 'bool', 'double', 'void', 'IDisposable', 'Action', 'MonoBehaviour'];

function highlightLine(line: string): (string | JSX.Element)[] {
  const parts: (string | JSX.Element)[] = [];
  // Simple comment check
  const commentIdx = line.indexOf('//');
  const code = commentIdx >= 0 ? line.slice(0, commentIdx) : line;
  const comment = commentIdx >= 0 ? line.slice(commentIdx) : '';

  // Tokenize code
  const tokens = code.split(/(\b|\s+|[.(),;=<>{}[\]])/);
  tokens.forEach((t, i) => {
    if (keywords.includes(t)) {
      parts.push(<span key={i} style={{ color: '#c792ea' }}>{t}</span>);
    } else if (types.includes(t)) {
      parts.push(<span key={i} style={{ color: '#82aaff' }}>{t}</span>);
    } else if (/^".*"$/.test(t)) {
      parts.push(<span key={i} style={{ color: '#c3e88d' }}>{t}</span>);
    } else if (t.startsWith('"')) {
      parts.push(<span key={i} style={{ color: '#c3e88d' }}>{t}</span>);
    } else {
      parts.push(t);
    }
  });

  if (comment) {
    parts.push(<span key="comment" style={{ color: '#546e7a' }}>{comment}</span>);
  }
  return parts;
}

export default function CodeBlock({ code }: Props) {
  const lines = code.split('\n');
  return (
    <Box
      sx={{
        bgcolor: '#0d1117',
        borderRadius: 2,
        border: '1px solid rgba(0,212,255,0.15)',
        p: 3,
        overflow: 'auto',
        maxWidth: '100%',
      }}
    >
      <pre style={{ margin: 0 }}>
        {lines.map((line, i) => (
          <Typography
            key={i}
            component="div"
            sx={{
              fontFamily: "'JetBrains Mono', monospace",
              fontSize: '0.82rem',
              lineHeight: 1.8,
              color: '#d4d4d4',
              whiteSpace: 'pre',
            }}
          >
            <span style={{ color: '#3a3f58', userSelect: 'none', marginRight: 16 }}>
              {String(i + 1).padStart(2)}
            </span>
            {highlightLine(line)}
          </Typography>
        ))}
      </pre>
    </Box>
  );
}
