// Lesson 4: Button
// A reusable Button component styled based on its "variant" prop.

interface ButtonProps {
  text: string;
  variant?: "primary" | "secondary" | "danger";
  disabled?: boolean;
  onClick?: () => void;
}

// Lesson 9: Default Props
// "variant" and "disabled" have default values, so they are optional
// when this component is used.
function Button({ text, variant = "primary", disabled = false, onClick }: ButtonProps) {
  return (
    <button
      className={`btn btn-${variant}`}
      disabled={disabled}
      onClick={onClick}
    >
      {text}
    </button>
  );
}

export default Button;
