// Lesson 1: Basic Props
// Props let a parent component pass data down to a child component.
// Here we pass three different primitive types: string, number, boolean.

interface BasicPropsProps {
  username: string;
  age: number;
  isOnline: boolean;
}

function BasicProps({ username, age, isOnline }: BasicPropsProps) {
  return (
    <div className="lesson">
      <h2>1. Basic Props (string, number, boolean)</h2>
      <p className="explanation">
        Props are read-only inputs passed from a parent to a child component.
      </p>
      <p>Username: {username}</p>
      <p>Age: {age}</p>
      <p>Status: {isOnline ? "🟢 Online" : "⚪ Offline"}</p>
    </div>
  );
}

export default BasicProps;
