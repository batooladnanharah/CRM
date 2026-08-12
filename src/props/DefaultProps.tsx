// Lesson 9: Default Props
// If a prop is optional, we can give it a default value using a
// default parameter. Callers may omit "greeting" and still get sensible output.

interface DefaultPropsProps {
  name: string;
  greeting?: string;
}

function DefaultProps({ name, greeting = "Welcome" }: DefaultPropsProps) {
  return (
    <p>
      {greeting}, {name}!
    </p>
  );
}

export default DefaultProps;
