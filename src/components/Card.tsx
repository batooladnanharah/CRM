import type { ReactNode } from "react";

type CardProps = {
  children: ReactNode;
};

function Card({ children }: CardProps) {
  return (
    <div
      style={{
        border: "1px solid gray",
        padding: "16px",
        borderRadius: "8px",
        marginBottom: "15px",
      }}
    >
      {children}
    </div>
  );
}

export default Card;