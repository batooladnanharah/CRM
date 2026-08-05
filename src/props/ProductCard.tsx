// Lesson 3: ProductCard
// Shows how multiple typed props can build a small product summary.

interface ProductCardProps {
  productName: string;
  price: number;
  stock: number;
  category: string;
}

function ProductCard({ productName, price, stock, category }: ProductCardProps) {
  const isInStock = stock > 0;

  return (
    <div className="product-card">
      <h3>{productName}</h3>
      <p>Category: {category}</p>
      <p>Price: ${price}</p>
      <p className={isInStock ? "in-stock" : "out-of-stock"}>
        {isInStock ? `In stock (${stock})` : "Out of stock"}
      </p>
    </div>
  );
}

export default ProductCard;
