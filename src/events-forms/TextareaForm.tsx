// Lesson 10: Textarea with Character Count
// Textareas are controlled just like inputs, via value + onChange.

import { useState } from "react";

const MAX_LENGTH = 200;

function TextareaForm() {
  const [description, setDescription] = useState("");

  return (
    <div>
      <textarea
        className="ef-textarea"
        rows={4}
        maxLength={MAX_LENGTH}
        placeholder="Enter a description..."
        value={description}
        onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setDescription(e.target.value)}
      />
      <p>
        Characters: {description.length} / {MAX_LENGTH}
      </p>
    </div>
  );
}

export default TextareaForm;
