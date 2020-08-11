<template>
  <div class="container">
    <div>
      <h1 class="title">
        ProgramTrackerWeb
      </h1>
      <form @submit.prevent="vis">
        <label for="user">User:</label>
        <input id="user" v-model="query.user" type="text" name="user" />
        <label for="start_time">Start:</label>
        <input
          id="start_time"
          v-model="query.start_time"
          type="datetime-local"
          name="start_time"
        />
        <label for="end_time">End:</label>
        <input
          id="end_time"
          v-model="query.end_time"
          type="datetime-local"
          name="end_time"
        />
        <button>
          Visualize
        </button>
      </form>
      <div id="timeline6" />
    </div>
  </div>
</template>

<script>
import axios from "axios";

export default {
  data() {
    return {
      data: [],
      query: {
        start_time: null,
        end_time: null,
        user: null
      }
    };
  },
  mounted() {
    console.log("unsern boi");
    console.log(this.$d3);
  },
  methods: {
    async vis() {
      this.data = (
        await axios.get("/api/test", {
          params: this.query
        })
      ).data;
      var chart = this.$d3
        .timeline()
        .beginning(new Date(this.query.start_time).getTime()) // we can optionally add beginning and ending times to speed up rendering a little
        .ending(new Date(this.query.end_time).getTime())
        .stack() // toggles graph stacking
        .margin({ left: 0, right: 0, top: 0, bottom: 0 })
        .showTimeAxisTick()
        .tickFormat({
          format: this.$d3.time.format("%H"),
          tickTime: this.$d3.time.hours,
          tickInterval: 2,
          tickSize: 2
        })
        .rowSeparators("grey");
      this.$d3.select("svg").remove();
      this.$d3
        .select("#timeline6")
        .append("svg")
        .attr("width", window.outerWidth - 200)
        .datum(this.data)
        .call(chart);
    }
  }
};
</script>

<style>
.container {
  margin: 0 auto;
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  text-align: center;
}

.title {
  font-family: "Quicksand", "Source Sans Pro", -apple-system, BlinkMacSystemFont,
    "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
  display: block;
  font-weight: 300;
  font-size: 100px;
  color: #35495e;
  letter-spacing: 1px;
}

.subtitle {
  font-weight: 300;
  font-size: 42px;
  color: #526488;
  word-spacing: 5px;
  padding-bottom: 15px;
}

.links {
  padding-top: 15px;
}
</style>
