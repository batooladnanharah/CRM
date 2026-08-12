// Mini Task Manager - TaskList
// Receives the task array through props and renders one TaskCard per
// task, forwarding the onToggle/onDelete callbacks down another level.

import TaskCard from "./TaskCard";

export type Task = {
  id: number;
  title: string;
  completed: boolean;
};

type TaskListProps = {
  tasks: Task[];
  onToggle: (id: number) => void;
  onDelete: (id: number) => void;
};

function TaskList({ tasks, onToggle, onDelete }: TaskListProps) {
  if (tasks.length === 0) {
    return <p>No tasks yet. Add one above!</p>;
  }

  return (
    <ul className="task-list">
      {tasks.map((task) => (
        <TaskCard key={task.id} task={task} onToggle={onToggle} onDelete={onDelete} />
      ))}
    </ul>
  );
}

export default TaskList;
