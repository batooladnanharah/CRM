// Mini Task Manager - parent component
// Owns the tasks state and passes it down to TaskList/TaskSummary,
// while passing callback functions down so children can request changes.

import { useState } from "react";
import TaskForm from "./TaskForm";
import TaskList from "./TaskList";
import TaskSummary from "./TaskSummary";
import type { Task } from "./TaskList";

let nextId = 1;

function TaskManager() {
  const [tasks, setTasks] = useState<Task[]>([]);

  function handleAddTask(title: string) {
    const newTask: Task = { id: nextId++, title, completed: false };
    setTasks((prev) => [...prev, newTask]);
  }

  function handleToggleTask(id: number) {
    setTasks((prev) =>
      prev.map((task) => (task.id === id ? { ...task, completed: !task.completed } : task))
    );
  }

  function handleDeleteTask(id: number) {
    setTasks((prev) => prev.filter((task) => task.id !== id));
  }

  const completedCount = tasks.filter((task) => task.completed).length;
  const remainingCount = tasks.length - completedCount;

  return (
    <div>
      <TaskForm onAddTask={handleAddTask} />
      <TaskSummary completedCount={completedCount} remainingCount={remainingCount} />
      <TaskList tasks={tasks} onToggle={handleToggleTask} onDelete={handleDeleteTask} />
    </div>
  );
}

export default TaskManager;
