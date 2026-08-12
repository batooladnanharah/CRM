// Lesson 8: Function Props
// A parent can pass a function down to a child so the child can
// notify the parent when something happens (e.g. a click).

interface FunctionPropsProps {
  onGreet: (name: string) => void;
}

function FunctionProps({ onGreet }: FunctionPropsProps) {
  return (
    <div>
      <p>Click the button below. It calls a function given by the parent.</p>
      <button className="btn btn-primary" onClick={() => onGreet("Batool")}>
        Say Hello
      </button>
    </div>
  );
}

export default FunctionProps;
