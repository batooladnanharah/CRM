// Lesson 3: Like Button
// Combines a boolean state (liked or not) with a number state (like count).

import { useState } from "react";

function LikeButton() {
  const [liked, setLiked] = useState(false);
  const [likeCount, setLikeCount] = useState(0);

  function handleLike() {
    setLiked(!liked);
    setLikeCount(liked ? likeCount - 1 : likeCount + 1);
  }

  return (
    <div>
      <button className="like-btn" onClick={handleLike} aria-label="Like">
        {liked ? "❤️" : "🤍"}
      </button>
      <span> {likeCount} likes</span>
    </div>
  );
}

export default LikeButton;
