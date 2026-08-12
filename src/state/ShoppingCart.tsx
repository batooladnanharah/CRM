// Lesson 7: Shopping Cart
// Demonstrates updating a number that has a minimum boundary (quantity can't go below 1).

import { useState } from "react";

function ShoppingCart() {
  const [quantity, setQuantity] = useState(1);
  const pricePerItem = 15;

  function decreaseQuantity() {
    setQuantity((prev) => Math.max(1, prev - 1));
  }

  return (
    <div>
      <p>Item: Notebook — ${pricePerItem} each</p>
      <button className="state-btn" onClick={decreaseQuantity}>
        -
      </button>
      <span> {quantity} </span>
      <button className="state-btn" onClick={() => setQuantity((prev) => prev + 1)}>
        +
      </button>
      <p>Total: ${quantity * pricePerItem}</p>
    </div>
  );
}

export default ShoppingCart;
