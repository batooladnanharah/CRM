// Lesson 7: Children Prop
// "children" lets a component wrap and display whatever JSX is placed
// between its opening and closing tags. This makes it reusable as a container.

import type { ReactNode } from "react";

interface CardProps {
  title: string;
  children: ReactNode;
}

function Card({ title, children }: CardProps) {
  return (
    <div className="card">
      <h3>{title}</h3>
      {children}
    </div>
  );
}

export default Card;
