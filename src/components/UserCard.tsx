import Card from "./Card";

type UserCardProps = {
  name: string;
  email: string;
};

function UserCard({ name, email }: UserCardProps) {
  return (
    <Card>
      <h2>{name}</h2>
      <p>{email}</p>
    </Card>
  );
}

export default UserCard;