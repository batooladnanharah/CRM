// Phase 5: React Events and Forms
// Each lesson below demonstrates one way of handling user interaction
// with React's event system and building controlled forms.

import BasicEvents from "./BasicEvents";
import EventObject from "./EventObject";
import EventArguments from "./EventArguments";
import ClickCounter from "./ClickCounter";
import ControlledInput from "./ControlledInput";
import MultipleInputs from "./MultipleInputs";
import CheckboxForm from "./CheckboxForm";
import RadioForm from "./RadioForm";
import SelectForm from "./SelectForm";
import TextareaForm from "./TextareaForm";
import UserForm from "./UserForm";
import FormValidation from "./FormValidation";
import TaskManager from "./TaskManager";
import "./events-forms.css";

function EventsFormsApp() {
  return (
    <div className="events-forms-app">
      <h1>Phase 5: React Events and Forms</h1>

      <div className="lesson">
        <h2>1. Basic Events</h2>
        <p className="explanation">
          onClick, onMouseEnter, onFocus/onBlur, and onKeyDown handlers.
        </p>
        <BasicEvents />
      </div>

      <div className="lesson">
        <h2>2. The Event Object</h2>
        <p className="explanation">
          Reading data off the synthetic event object with proper TypeScript types.
        </p>
        <EventObject />
      </div>

      <div className="lesson">
        <h2>3. Passing Arguments to Event Handlers</h2>
        <p className="explanation">
          Wrapping a handler in an arrow function to pass a custom argument like an id.
        </p>
        <EventArguments />
      </div>

      <div className="lesson">
        <h2>4. Click Counter</h2>
        <p className="explanation">
          Combines onClick with functional state updates from Phase 4.
        </p>
        <ClickCounter />
      </div>

      <div className="lesson">
        <h2>5. Controlled Input</h2>
        <p className="explanation">The input's value is controlled by React state.</p>
        <ControlledInput />
      </div>

      <div className="lesson">
        <h2>6. Multiple Inputs (One State Object)</h2>
        <p className="explanation">
          Name, email, and age stored together and updated without mutating state.
        </p>
        <MultipleInputs />
      </div>

      <div className="lesson">
        <h2>7. Checkbox</h2>
        <p className="explanation">A boolean state variable controls a checkbox.</p>
        <CheckboxForm />
      </div>

      <div className="lesson">
        <h2>8. Radio Buttons</h2>
        <p className="explanation">Only one radio button in a group can be selected.</p>
        <RadioForm />
      </div>

      <div className="lesson">
        <h2>9. Select Dropdown</h2>
        <p className="explanation">Choosing a country from a controlled select input.</p>
        <SelectForm />
      </div>

      <div className="lesson">
        <h2>10. Textarea with Character Count</h2>
        <p className="explanation">A controlled textarea that tracks its own length.</p>
        <TextareaForm />
      </div>

      <div className="lesson">
        <h2>11. Form Submission</h2>
        <p className="explanation">
          Using event.preventDefault() so submitting doesn't reload the page.
        </p>
        <UserForm />
      </div>

      <div className="lesson">
        <h2>12. Basic Form Validation</h2>
        <p className="explanation">
          Required fields and a minimum password length, validated without a library.
        </p>
        <FormValidation />
      </div>

      <div className="lesson">
        <h2>13. Mini Task Manager</h2>
        <p className="explanation">
          Add, complete, and delete tasks using state, props, and events together.
        </p>
        <TaskManager />
      </div>
    </div>
  );
}

export default EventsFormsApp;
