import * as React from 'react';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';

export const Expenses = () => {
  var [expenses, setExpenses] = useState(null);
  var [date, setDate] = useState(null);
  var { sessionId } = useParams();

  console.warn(sessionId);

  useEffect(() => {
    const response = date == null
        ? fetch(`expenses/${sessionId}/all`)
        : fetch(`expenses/${sessionId}/expenses-for-dates?start=${date.toJSON()}&end=${new Date().toJSON()}`);
    
    
    response.then(res => {
        res.json().then(data => {
          console.log(data);
          setExpenses(data);
        },
        err => console.log(err));
      },
      err => console.log(err));
  }, [date, setExpenses]);

    return (
      <div>
        <h1 id="tableLabel">Exepnses</h1>
        {expenses && (
          <div>
            <LocalizationProvider dateAdapter={AdapterDayjs}>
                <DatePicker value={date} onChange={(newValue) => setDate(newValue)} />
            </LocalizationProvider>
            <table className="table table-striped" aria-labelledby="tableLabel">
                <thead>
                <tr>
                    <th>Date</th>
                    <th>Name</th>
                    <th>Cost</th>
                </tr>
                </thead>
                <tbody>
                {expenses.map((forecast, i) =>
                    <tr key={i}>
                      <td>{new Date(forecast.date).toLocaleString()}</td>
                      <td>{forecast.name}</td>
                      <td>{forecast.cost}</td>
                    </tr>
                )}
                </tbody>
            </table>
          </div>)}
      </div>
    );
}
