import Card from "./Card";
import Button from "./Button";

type ProductCardProps = {
  name: string;
  price: number;
};

function ProductCard({ name, price }: ProductCardProps) {
  return (
    <Card>
      <h2>{name}</h2>
      <p>${price}</p>

      <Button text="Buy Now" />
    </Card>
  );
}

export default ProductCard;