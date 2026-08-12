// Lesson 2: UserCard
// A reusable component that displays user details passed in via props.

interface UserCardProps {
  name: string;
  age: number;
  country: string;
  image: string;
}

function UserCard({ name, age, country, image }: UserCardProps) {
  return (
    <div className="user-card">
      <img src={image} alt={name} />
      <div>
        <h3>{name}</h3>
        <p>Age: {age}</p>
        <p>Country: {country}</p>
      </div>
    </div>
  );
}

export default UserCard;
