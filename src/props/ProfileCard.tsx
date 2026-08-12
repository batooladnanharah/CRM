// Lesson 5: Passing an object as a prop
// Instead of many separate props, we can group related data into one object.

interface Profile {
  name: string;
  title: string;
  image: string;
}

interface ProfileCardProps {
  profile: Profile;
}

function ProfileCard({ profile }: ProfileCardProps) {
  return (
    <div className="profile-card">
      <img src={profile.image} alt={profile.name} />
      <div>
        <h3>{profile.name}</h3>
        <p>{profile.title}</p>
      </div>
    </div>
  );
}

export default ProfileCard;
